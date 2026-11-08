using Unity.Netcode;
using UnityEngine;

public class BombInteractor : NetworkBehaviour
{
    private const float DEFAULT_PLANT_DURATION = 3.5f;
    private const float DEFAULT_DEFUSE_DURATION = 5.0f;
    private const float INTERACTION_DISTANCE = 3.5f;

    [SerializeField] private Transform _interactOrigin;
    [SerializeField] private LayerMask _bombLayer;
    [SerializeField] private LayerMask _siteLayer;
    [SerializeField] private PlayerTeam _playerTeam;

    [SerializeField] private float _plantDuration = DEFAULT_PLANT_DURATION;
    [SerializeField] private float _defuseDuration = DEFAULT_DEFUSE_DURATION;

    private Bomb _activeBomb;
    private BombSite _currentSite;
    private string _currentSiteId = string.Empty;
    private bool _isPlanting;
    private bool _isDefusing;
    private float _currentInteractionTimer;
    private float _pickupCooldownTime;

    public bool HasBomb => _activeBomb != null && _activeBomb.CurrentState == BombState.Carried && _activeBomb.CurrentCarrierId == OwnerClientId;
    public bool IsCarryingBomb => HasBomb;
    public bool IsInSite => !string.IsNullOrEmpty(_currentSiteId) && _currentSite != null;
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
        if (_interactOrigin == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            _interactOrigin = cam != null ? cam.transform : transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ResetSiteState();
        LocateSceneBomb();
    }

    private void OnEnable()
    {
        ResetSiteState();
    }

    private void OnDisable()
    {
        ResetSiteState();
    }

    private void Update()
    {
        if (!IsOwner) return;

        LocateSceneBomb();
        ValidateCurrentSite();
        HandleManualPickup();
        HandleDropInput();
        HandlePlantAndDefuse();
    }

    public void ResetSiteState()
    {
        _currentSite = null;
        _currentSiteId = string.Empty;
        CancelInteraction();
    }

    private void ValidateCurrentSite()
    {
        if (_currentSite == null)
        {
            if (!string.IsNullOrEmpty(_currentSiteId))
            {
                _currentSiteId = string.Empty;
            }
            return;
        }

        Collider siteCol = _currentSite.GetComponent<Collider>();
        if (siteCol == null || !siteCol.enabled || !siteCol.gameObject.activeInHierarchy)
        {
            ResetSiteState();
            return;
        }

        Vector3 closestPoint = siteCol.ClosestPoint(transform.position);
        float sqrDistanceToSite = (closestPoint - transform.position).sqrMagnitude;

        if (sqrDistanceToSite > 1.0f)
        {
            ResetSiteState();
        }
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

    private void HandleManualPickup()
    {
        if (HasBomb || !HasNearbyBomb || InputManager.Instance == null) return;
        if (Time.time < _pickupCooldownTime) return;

        if (InputManager.Instance.WasInteractTriggered)
        {
            _pickupCooldownTime = Time.time + 0.4f;
            RequestPickupBombServerRpc();
        }
    }

    private void HandleDropInput()
    {
        if (!HasBomb) return;

        if (InputManager.Instance != null && InputManager.Instance.WasDropTriggered)
        {
            _pickupCooldownTime = Time.time + 0.8f;
            Vector3 dropPos = transform.position + (transform.forward * 1.2f) + (Vector3.up * 0.1f);
            RequestDropBombServerRpc(dropPos, transform.rotation);
        }
    }

    private void HandlePlantAndDefuse()
    {
        if (InputManager.Instance == null || _activeBomb == null) return;

        bool isHoldingInteract = InputManager.Instance.IsInteractPressed;

        if (HasBomb && IsInSite && _playerTeam != null && _playerTeam.CurrentTeam.Value == Team.Red)
        {
            if (isHoldingInteract)
            {
                if (!_isPlanting)
                {
                    _isPlanting = true;
                    _currentInteractionTimer = 0f;
                    RequestStartPlantAudioServerRpc();
                }

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
                if (!_isDefusing)
                {
                    _isDefusing = true;
                    _currentInteractionTimer = 0f;
                    RequestStartDefuseAudioServerRpc();
                }

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
            _currentSite = site;
            _currentSiteId = site.SiteId;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BombSite site = other.GetComponent<BombSite>();
        if (site != null && _currentSiteId == site.SiteId)
        {
            _currentSite = null;
            _currentSiteId = string.Empty;
            if (_isPlanting) CancelInteraction();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestStartPlantAudioServerRpc()
    {
        BroadcastBombSoundClientRpc(4);
    }

    [Rpc(SendTo.Server)]
    private void RequestStartDefuseAudioServerRpc()
    {
        BroadcastBombSoundClientRpc(5);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void BroadcastBombSoundClientRpc(byte soundType)
    {
        LocateSceneBomb();
        if (_activeBomb != null)
        {
            _activeBomb.PlaySoundEffect(soundType);
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

        BombSite[] allSites = FindObjectsByType<BombSite>();
        bool isValidSite = false;

        for (int i = 0; i < allSites.Length; i++)
        {
            if (allSites[i] != null && allSites[i].SiteId == siteId)
            {
                Collider col = allSites[i].GetComponent<Collider>();
                if (col != null)
                {
                    float distSqr = (col.ClosestPoint(plantPosition) - plantPosition).sqrMagnitude;
                    if (distSqr <= 1.5f)
                    {
                        isValidSite = true;
                        break;
                    }
                }
            }
        }

        if (!isValidSite)
        {
            Debug.LogWarning($"[Bomb] Rechazado intento de plantado ilegal fuera de la zona {siteId} en {plantPosition}");
            return;
        }

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