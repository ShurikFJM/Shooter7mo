using Unity.Netcode;
using UnityEngine;

public class BombInteractor : NetworkBehaviour
{
    private const float DEFAULT_PLANT_DURATION = 3.5f;
    private const float DEFAULT_DEFUSE_DURATION = 5.0f;
    private const float INTERACTION_DISTANCE = 3.5f;
    private const float AUTO_PICKUP_RADIUS = 2.2f;

    [SerializeField] private Camera _interactOrigin;
    [SerializeField] private LayerMask _bombLayer;
    [SerializeField] private LayerMask _siteLayer;
    [SerializeField] private PlayerTeam _playerTeam;

    [SerializeField] private float _plantDuration = DEFAULT_PLANT_DURATION;
    [SerializeField] private float _defuseDuration = DEFAULT_DEFUSE_DURATION;

    private Bomb _activeBomb;
    private string _currentSiteId = string.Empty;
    private bool _isPlanting;
    private bool _isDefusing;
    private float _currentInteractionTimer;
    private float _lastPickupRequestTime;

    public bool HasBomb => _activeBomb != null && _activeBomb.CurrentState == BombState.Carried && _activeBomb.CurrentCarrierId == OwnerClientId;
    public bool IsCarryingBomb => HasBomb;
    public bool IsInSite => !string.IsNullOrEmpty(_currentSiteId);
    public bool IsPlanting => _isPlanting;
    public bool IsDefusing => _isDefusing;
    public bool IsInteracting => _isPlanting || _isDefusing;

    public float PlantProgressNormalized => (_isPlanting && _plantDuration > 0f) ? Mathf.Clamp01(_currentInteractionTimer / _plantDuration) : 0f;
    public float DefuseProgressNormalized => (_isDefusing && _defuseDuration > 0f) ? Mathf.Clamp01(_currentInteractionTimer / _defuseDuration) : 0f;
    public float InteractionProgressNormalized => _isPlanting ? PlantProgressNormalized : DefuseProgressNormalized;

    public bool HasNearbyPlantedBomb
    {
        get
        {
            if (_activeBomb == null || _activeBomb.CurrentState != BombState.Planted) return false;
            if (_playerTeam != null && _playerTeam.CurrentTeam.Value != Team.Blue) return false;
            return GetHorizontalDistanceSqr(transform.position, _activeBomb.transform.position) <= (INTERACTION_DISTANCE * INTERACTION_DISTANCE);
        }
    }

    public bool HasNearbyBomb
    {
        get
        {
            if (_activeBomb == null || _activeBomb.CurrentState != BombState.OnGround) return false;
            if (_playerTeam != null && _playerTeam.CurrentTeam.Value != Team.Red) return false;
            return GetHorizontalDistanceSqr(transform.position, _activeBomb.transform.position) <= (INTERACTION_DISTANCE * INTERACTION_DISTANCE);
        }
    }

    private void Awake()
    {
        if (_playerTeam == null) _playerTeam = GetComponent<PlayerTeam>();
        if (_interactOrigin == null) _interactOrigin = GetComponentInChildren<Camera>(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        LocateSceneBomb();
    }

    private void Update()
    {
        if (!IsOwner) return;

        LocateSceneBomb();
        CheckAutoPickup();
        HandleManualPickup();
        HandleDropInput();
        HandlePlantAndDefuse();
    }

    private void LocateSceneBomb()
    {
        if (_activeBomb == null)
        {
            _activeBomb = FindAnyObjectByType<Bomb>();
        }
    }

    private float GetHorizontalDistanceSqr(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return (dx * dx) + (dz * dz);
    }

    private void CheckAutoPickup()
    {
        if (_activeBomb == null || HasBomb) return;
        if (_playerTeam == null || _playerTeam.CurrentTeam.Value != Team.Red) return;
        if (Time.time - _lastPickupRequestTime < 0.25f) return;

        if (_activeBomb.CurrentState == BombState.OnGround)
        {
            float horizontalDistSqr = GetHorizontalDistanceSqr(transform.position, _activeBomb.transform.position);
            if (horizontalDistSqr <= (AUTO_PICKUP_RADIUS * AUTO_PICKUP_RADIUS))
            {
                _lastPickupRequestTime = Time.time;
                RequestPickupBombServerRpc();
            }
        }
    }

    private void HandleManualPickup()
    {
        if (HasBomb || !HasNearbyBomb || InputManager.Instance == null) return;

        if (InputManager.Instance.WasInteractTriggered)
        {
            _lastPickupRequestTime = Time.time;
            RequestPickupBombServerRpc();
        }
    }

    private void HandleDropInput()
    {
        if (!HasBomb) return;

        if (InputManager.Instance != null && InputManager.Instance.WasDropTriggered)
        {
            Vector3 dropPos = transform.position + (transform.forward * 1.2f) + (Vector3.up * 0.1f);
            RequestDropBombServerRpc(dropPos, transform.rotation);
        }
    }

    private void HandlePlantAndDefuse()
    {
        if (InputManager.Instance == null || _activeBomb == null) return;

        bool isHoldingInteract = InputManager.Instance.IsInteractPressed;

        if (HasBomb && !string.IsNullOrEmpty(_currentSiteId) && _playerTeam != null && _playerTeam.CurrentTeam.Value == Team.Red)
        {
            if (isHoldingInteract)
            {
                _isPlanting = true;
                _currentInteractionTimer += Time.deltaTime;

                if (_currentInteractionTimer >= _plantDuration)
                {
                    Vector3 plantPos = transform.position;
                    Quaternion plantRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                    RequestPlantBombServerRpc(_currentSiteId, plantPos, plantRot);
                    CancelInteraction();
                }
                return;
            }
        }

        if (_playerTeam != null && _playerTeam.CurrentTeam.Value == Team.Blue && _activeBomb.CurrentState == BombState.Planted)
        {
            float horizontalDistSqr = GetHorizontalDistanceSqr(transform.position, _activeBomb.transform.position);
            if (horizontalDistSqr <= (INTERACTION_DISTANCE * INTERACTION_DISTANCE) && isHoldingInteract)
            {
                _isDefusing = true;
                _currentInteractionTimer += Time.deltaTime;

                if (_currentInteractionTimer >= _defuseDuration)
                {
                    RequestDefuseBombServerRpc();
                    CancelInteraction();
                }
                return;
            }
        }

        if (_isPlanting || _isDefusing)
        {
            CancelInteraction();
        }
    }

    private void CancelInteraction()
    {
        _isPlanting = false;
        _isDefusing = false;
        _currentInteractionTimer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        BombSite site = other.GetComponent<BombSite>();
        if (site != null)
        {
            _currentSiteId = site.SiteId;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BombSite site = other.GetComponent<BombSite>();
        if (site != null && _currentSiteId == site.SiteId)
        {
            _currentSiteId = string.Empty;
            if (_isPlanting) CancelInteraction();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SyncBombStateClientRpc(byte stateByte, ulong carrierId, Vector3 targetPos, Quaternion targetRot, double plantServerTime, byte soundType)
    {
        LocateSceneBomb();
        if (_activeBomb != null)
        {
            _activeBomb.ApplySyncedBombState((BombState)stateByte, carrierId, targetPos, targetRot, plantServerTime, soundType);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestPickupBombServerRpc(RpcParams rpcParams = default)
    {
        LocateSceneBomb();
        if (_activeBomb == null) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        _activeBomb.ServerPickup(senderId);
    }

    [Rpc(SendTo.Server)]
    private void RequestDropBombServerRpc(Vector3 dropPosition, Quaternion dropRotation)
    {
        LocateSceneBomb();
        if (_activeBomb == null) return;

        _activeBomb.ServerDrop(dropPosition, dropRotation);
    }

    [Rpc(SendTo.Server)]
    private void RequestPlantBombServerRpc(string siteId, Vector3 plantPosition, Quaternion plantRotation)
    {
        LocateSceneBomb();
        if (_activeBomb == null) return;

        _activeBomb.ServerPlant(siteId, plantPosition, plantRotation);
    }

    [Rpc(SendTo.Server)]
    private void RequestDefuseBombServerRpc()
    {
        LocateSceneBomb();
        if (_activeBomb == null) return;

        _activeBomb.ServerDefuse();
    }
}