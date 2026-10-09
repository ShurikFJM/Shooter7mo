using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class BombInteractor : NetworkBehaviour
{
    private const float RAYCAST_ORIGIN_OFFSET = 0.5f;
    private const float GROUND_CHECK_DISTANCE = 2f;
    private const float SITE_DETECTION_RADIUS = 0.5f;
    private const int MAX_BUFFER_HITS = 8;

    [SerializeField] private Transform _interactOrigin;
    [SerializeField] private float _interactRange = 3.5f;
    [SerializeField] private LayerMask _bombLayer;
    [SerializeField] private LayerMask _siteLayer;
    [SerializeField] private float _plantHoldTime = 4f;
    [SerializeField] private float _defuseHoldTime = 5f;
    [SerializeField] private PlayerTeam _playerTeam;
    [SerializeField] private AudioSource _interactorAudioSource;
    [SerializeField] private AudioClip _startPlantingClip;
    [SerializeField] private AudioClip _startDefusingClip;

    private readonly Collider[] _bombHitBuffer = new Collider[MAX_BUFFER_HITS];
    private readonly Collider[] _siteHitBuffer = new Collider[MAX_BUFFER_HITS];

    private Bomb _cachedBomb;
    private Bomb _carriedBomb;
    private Bomb _nearbyBomb;
    private Bomb _nearbyPlantedBomb;
    private BombSite _currentSite;
    private float _plantProgress;
    private bool _isPlanting;
    private float _defuseProgress;
    private bool _isDefusing;

    public bool IsCarryingBomb => _carriedBomb != null &&
                                  _carriedBomb.bombState.Value == BombState.Carried &&
                                  _carriedBomb.carrierClientId.Value == NetworkManager.Singleton.LocalClientId;

    public bool IsPlanting => _isPlanting;
    public float PlantProgressNormalized => _plantHoldTime > 0f ? _plantProgress / _plantHoldTime : 0f;
    public bool IsDefusing => _isDefusing;
    public float DefuseProgressNormalized => _defuseHoldTime > 0f ? _defuseProgress / _defuseHoldTime : 0f;
    public bool HasNearbyBomb => _nearbyBomb != null;
    public bool HasNearbyPlantedBomb => _nearbyPlantedBomb != null;
    public bool IsInSite => _currentSite != null;

    private void Awake()
    {
        if (_interactOrigin == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            _interactOrigin = playerCamera != null ? playerCamera.transform : transform;
        }

        if (_playerTeam == null) _playerTeam = GetComponent<PlayerTeam>();

        if (_interactorAudioSource == null)
        {
            _interactorAudioSource = GetComponent<AudioSource>();
            if (_interactorAudioSource == null)
            {
                _interactorAudioSource = gameObject.AddComponent<AudioSource>();
                _interactorAudioSource.spatialBlend = 1f;
                _interactorAudioSource.playOnAwake = false;
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        LocateSceneBomb();
        ValidateCarriedBombAuthority();

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            CancelActions();
            return;
        }

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            CancelActions();
            return;
        }

        if (RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value != RoundPhase.InProgress)
        {
            CancelActions();
            return;
        }

        DetectNearbyDroppedBomb();
        DetectNearbyPlantedBomb();
        DetectCurrentBombSite();
        ExecutePlantingTimer();
        ExecuteDefusingTimer();
    }

    private void LocateSceneBomb()
    {
        if (_cachedBomb == null) _cachedBomb = FindAnyObjectByType<Bomb>();
    }

    private void ValidateCarriedBombAuthority()
    {
        if (_carriedBomb == null)
        {
            if (_cachedBomb != null &&
                _cachedBomb.bombState.Value == BombState.Carried &&
                _cachedBomb.carrierClientId.Value == NetworkManager.Singleton.LocalClientId)
            {
                _carriedBomb = _cachedBomb;
            }
        }
        else
        {
            if (_carriedBomb.bombState.Value != BombState.Carried ||
                _carriedBomb.carrierClientId.Value != NetworkManager.Singleton.LocalClientId)
            {
                _carriedBomb = null;
                if (_isPlanting) StopPlantingProcess();
            }
        }
    }

    private void DetectNearbyDroppedBomb()
    {
        if (IsCarryingBomb || _playerTeam == null)
        {
            _nearbyBomb = null;
            return;
        }

        if (_cachedBomb != null && _cachedBomb.bombState.Value == BombState.Dropped)
        {
            if (_playerTeam.CurrentTeam.Value == _cachedBomb.BombCarrierTeam)
            {
                float distance = Vector3.Distance(_interactOrigin.position, _cachedBomb.transform.position);
                if (distance <= _interactRange)
                {
                    _nearbyBomb = _cachedBomb;
                    return;
                }
            }
        }

        _nearbyBomb = null;
    }

    private void DetectNearbyPlantedBomb()
    {
        if (_playerTeam == null || _cachedBomb == null)
        {
            _nearbyPlantedBomb = null;
            return;
        }

        if (_cachedBomb.bombState.Value == BombState.Planted || _cachedBomb.bombState.Value == BombState.Defusing)
        {
            if (_playerTeam.CurrentTeam.Value == _cachedBomb.DefuseTeam)
            {
                float distance = Vector3.Distance(_interactOrigin.position, _cachedBomb.transform.position);
                if (distance <= _interactRange)
                {
                    _nearbyPlantedBomb = _cachedBomb;
                    return;
                }
            }
        }

        _nearbyPlantedBomb = null;
    }

    private void DetectCurrentBombSite()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, SITE_DETECTION_RADIUS, _siteHitBuffer, _siteLayer);
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
            CancelActions();
        }
    }

    private void ProcessInteractionInput()
    {
        if (IsCarryingBomb && _currentSite != null)
        {
            StartPlantingProcess();
        }
        else if (IsCarryingBomb)
        {
            RequestDropExecution();
        }
        else if (_nearbyPlantedBomb != null && _nearbyPlantedBomb.bombState.Value == BombState.Planted)
        {
            StartDefusingProcess();
        }
        else if (_nearbyBomb != null)
        {
            RequestPickupExecution(_nearbyBomb);
        }
    }

    private void ExecutePlantingTimer()
    {
        if (!_isPlanting) return;

        if (_currentSite == null || !IsCarryingBomb)
        {
            StopPlantingProcess();
            return;
        }

        _plantProgress += Time.deltaTime;
        if (_plantProgress >= _plantHoldTime)
        {
            CompletePlantingProcess();
            StopPlantingProcess();
        }
    }

    private void ExecuteDefusingTimer()
    {
        if (!_isDefusing) return;

        bool isAnotherPlayerDefusing = _nearbyPlantedBomb != null &&
                                       _nearbyPlantedBomb.bombState.Value == BombState.Defusing &&
                                       _nearbyPlantedBomb.defuserClientId.Value != Bomb.UNASSIGNED_CARRIER_ID &&
                                       _nearbyPlantedBomb.defuserClientId.Value != NetworkManager.Singleton.LocalClientId;

        if (_nearbyPlantedBomb == null ||
            (_nearbyPlantedBomb.bombState.Value != BombState.Planted && _nearbyPlantedBomb.bombState.Value != BombState.Defusing) ||
            isAnotherPlayerDefusing)
        {
            StopDefusingProcess();
            return;
        }

        _defuseProgress += Time.deltaTime;
        if (_defuseProgress >= _defuseHoldTime)
        {
            CompleteDefusingProcess();
            StopDefusingProcess();
        }
    }

    private void StartPlantingProcess()
    {
        _isPlanting = true;
        _plantProgress = 0f;
        PlayInteractionSoundServerRpc(true);
    }

    private void StopPlantingProcess()
    {
        _isPlanting = false;
        _plantProgress = 0f;
    }

    private void StartDefusingProcess()
    {
        if (_nearbyPlantedBomb == null) return;

        _isDefusing = true;
        _defuseProgress = 0f;

        PlayInteractionSoundServerRpc(false);
        _nearbyPlantedBomb.RequestStartDefuseServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void StopDefusingProcess()
    {
        if (_isDefusing && _nearbyPlantedBomb != null)
        {
            _nearbyPlantedBomb.RequestStopDefuseServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        _isDefusing = false;
        _defuseProgress = 0f;
    }

    private void CancelActions()
    {
        StopPlantingProcess();
        StopDefusingProcess();
    }

    private void CompletePlantingProcess()
    {
        if (_carriedBomb == null || _currentSite == null || !IsCarryingBomb) return;

        Vector3 groundPlantPosition = transform.position;
        Vector3 raycastOrigin = transform.position + Vector3.up * RAYCAST_ORIGIN_OFFSET;

        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit groundHit, GROUND_CHECK_DISTANCE))
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
        if (_nearbyPlantedBomb == null) return;
        _nearbyPlantedBomb.RequestDefuseServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void RequestPickupExecution(Bomb targetBomb)
    {
        targetBomb.RequestPickupServerRpc(NetworkManager.Singleton.LocalClientId);
        _carriedBomb = targetBomb;
    }

    private void RequestDropExecution()
    {
        if (_carriedBomb == null) return;

        Vector3 dropOrigin = _interactOrigin != null ? _interactOrigin.position : (transform.position + Vector3.up * 1.2f);
        Vector3 throwDirection = transform.forward;
        throwDirection.y = 0f;
        throwDirection = throwDirection.sqrMagnitude > 0.001f ? throwDirection.normalized : transform.forward;

        _carriedBomb.RequestDropServerRpc(NetworkManager.Singleton.LocalClientId, dropOrigin, throwDirection);

        _carriedBomb = null;
        StopPlantingProcess();
    }

    [Rpc(SendTo.Server)]
    private void PlayInteractionSoundServerRpc(bool isPlanting)
    {
        PlayInteractionSoundClientRpc(isPlanting);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayInteractionSoundClientRpc(bool isPlanting)
    {
        AudioClip clip = isPlanting ? _startPlantingClip : _startDefusingClip;
        if (clip != null && _interactorAudioSource != null)
        {
            _interactorAudioSource.PlayOneShot(clip);
        }
    }
}