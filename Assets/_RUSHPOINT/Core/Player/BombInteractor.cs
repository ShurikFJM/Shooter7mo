using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class BombInteractor : NetworkBehaviour
{
    private const float _RAYCAST_ORIGIN_OFFSET = 0.5f;
    private const float _GROUND_CHECK_DISTANCE = 2f;
    private const float _SITE_DETECTION_RADIUS = 0.5f;
    private const int _MAX_BUFFER_HITS = 8;

    [SerializeField] private Transform _interactOrigin;
    [SerializeField] private float _interactRange = 3f;
    [SerializeField] private LayerMask _bombLayer;
    [SerializeField] private LayerMask _siteLayer;
    [SerializeField] private float _plantHoldTime = 4f;
    [SerializeField] private float _defuseHoldTime = 5f;

    private readonly Collider[] _bombHitBuffer = new Collider[_MAX_BUFFER_HITS];
    private readonly Collider[] _siteHitBuffer = new Collider[_MAX_BUFFER_HITS];

    private Bomb _carriedBomb;
    private Bomb _nearbyBomb;
    private BombSite _currentSite;
    private float _actionProgress;
    private bool _isPlanting;
    private bool _isDefusing;

    public bool IsCarryingBomb => _carriedBomb != null &&
                                  _carriedBomb.State.Value == BombState.Carried &&
                                  _carriedBomb.CarrierClientId.Value == NetworkManager.Singleton.LocalClientId;

    public bool IsPlanting => _isPlanting;
    public bool IsDefusing => _isDefusing;
    public float ActionProgressNormalized => _isDefusing
        ? (_defuseHoldTime > 0f ? _actionProgress / _defuseHoldTime : 0f)
        : (_plantHoldTime > 0f ? _actionProgress / _plantHoldTime : 0f);

    public bool HasNearbyBomb => _nearbyBomb != null;
    public bool HasNearbyPlantedBomb => _nearbyBomb != null && _nearbyBomb.State.Value == BombState.Planted;
    public bool IsInSite => _currentSite != null;

    private void Awake()
    {
        if (_interactOrigin == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            _interactOrigin = playerCamera != null ? playerCamera.transform : transform;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        ValidateCarriedBombAuthority();

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            CancelActionProcess();
            return;
        }

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            CancelActionProcess();
            return;
        }

        DetectNearbyBombs();
        DetectCurrentBombSite();
        ExecuteActionTimer();
    }

    private void ValidateCarriedBombAuthority()
    {
        if (_carriedBomb == null)
        {
            Bomb[] allBombs = FindObjectsByType<Bomb>();
            for (int i = 0; i < allBombs.Length; i++)
            {
                if (allBombs[i].State.Value == BombState.Carried &&
                    allBombs[i].CarrierClientId.Value == NetworkManager.Singleton.LocalClientId)
                {
                    _carriedBomb = allBombs[i];
                    break;
                }
            }
        }
        else
        {
            if (_carriedBomb.State.Value != BombState.Carried ||
                _carriedBomb.CarrierClientId.Value != NetworkManager.Singleton.LocalClientId)
            {
                _carriedBomb = null;
                if (_isPlanting)
                {
                    CancelActionProcess();
                }
            }
        }
    }

    private void DetectNearbyBombs()
    {
        if (IsCarryingBomb)
        {
            _nearbyBomb = null;
            return;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(_interactOrigin.position, _interactRange, _bombHitBuffer, _bombLayer);
        _nearbyBomb = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider colliderItem = _bombHitBuffer[i];
            Bomb detectedBomb = colliderItem.GetComponentInParent<Bomb>();
            if (detectedBomb == null) continue;

            if (detectedBomb.State.Value != BombState.Dropped && detectedBomb.State.Value != BombState.Planted) continue;

            float currentDistance = Vector3.Distance(_interactOrigin.position, detectedBomb.transform.position);
            if (currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                _nearbyBomb = detectedBomb;
            }
        }
    }

    private void DetectCurrentBombSite()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _SITE_DETECTION_RADIUS, _siteHitBuffer, _siteLayer);
        _currentSite = null;

        for (int i = 0; i < hitCount; i++)
        {
            BombSite detectedSite = _siteHitBuffer[i].GetComponent<BombSite>();
            if (detectedSite != null)
            {
                _currentSite = detectedSite;
                break;
            }
        }
    }

    public void OnInteract(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
        {
            ProcessInteractionInput();
        }
        else
        {
            CancelActionProcess();
        }
    }

    private void ProcessInteractionInput()
    {
        if (_nearbyBomb != null && _nearbyBomb.State.Value == BombState.Planted)
        {
            StartDefusingProcess();
        }
        else if (IsCarryingBomb && _currentSite != null)
        {
            StartPlantingProcess();
        }
        else if (IsCarryingBomb)
        {
            RequestDropExecution();
        }
        else if (_nearbyBomb != null && _nearbyBomb.State.Value == BombState.Dropped)
        {
            RequestPickupExecution(_nearbyBomb);
        }
    }

    private void ExecuteActionTimer()
    {
        if (_isPlanting)
        {
            if (_currentSite == null || !IsCarryingBomb)
            {
                CancelActionProcess();
                return;
            }

            _actionProgress += Time.deltaTime;
            if (_actionProgress >= _plantHoldTime)
            {
                CompletePlantingProcess();
                CancelActionProcess();
            }
        }
        else if (_isDefusing)
        {
            if (_nearbyBomb == null || _nearbyBomb.State.Value != BombState.Planted)
            {
                CancelActionProcess();
                return;
            }

            _actionProgress += Time.deltaTime;
            if (_actionProgress >= _defuseHoldTime)
            {
                CompleteDefusingProcess();
                CancelActionProcess();
            }
        }
    }

    private void StartPlantingProcess()
    {
        _isPlanting = true;
        _isDefusing = false;
        _actionProgress = 0f;
    }

    private void StartDefusingProcess()
    {
        _isDefusing = true;
        _isPlanting = false;
        _actionProgress = 0f;
    }

    private void CancelActionProcess()
    {
        _isPlanting = false;
        _isDefusing = false;
        _actionProgress = 0f;
    }

    private void CompletePlantingProcess()
    {
        if (_carriedBomb == null || _currentSite == null || !IsCarryingBomb) return;

        Vector3 groundPlantPosition = transform.position;
        Vector3 raycastOrigin = transform.position + Vector3.up * _RAYCAST_ORIGIN_OFFSET;

        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit groundHit, _GROUND_CHECK_DISTANCE))
        {
            groundPlantPosition = groundHit.point;
        }

        Quaternion playerFacingRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        NetworkBehaviourReference siteReference = new NetworkBehaviourReference(_currentSite);

        _carriedBomb.RequestPlantServerRpc(
            NetworkManager.Singleton.LocalClientId,
            siteReference,
            groundPlantPosition,
            playerFacingRotation
        );

        _carriedBomb = null;
    }

    private void CompleteDefusingProcess()
    {
        if (_nearbyBomb == null || _nearbyBomb.State.Value != BombState.Planted) return;

        _nearbyBomb.RequestDefuseServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void RequestPickupExecution(Bomb targetBomb)
    {
        targetBomb.RequestPickupServerRpc(NetworkManager.Singleton.LocalClientId);
        _carriedBomb = targetBomb;
    }

    private void RequestDropExecution()
    {
        if (_carriedBomb == null) return;
        _carriedBomb.RequestDropServerRpc(NetworkManager.Singleton.LocalClientId, transform.position);
        _carriedBomb = null;
        CancelActionProcess();
    }
}