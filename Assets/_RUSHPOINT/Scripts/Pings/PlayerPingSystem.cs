using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPingSystem : NetworkBehaviour
{
    public enum PingType
    {
        Normal = 0,
        Danger = 1,
        Group = 2
    }

    private const float _RADIAL_CENTER_DEADZONE_SQR = 2500f;
    private const float _NORMAL_PING_OFFSET = 0.2f;
    private const float _RAY_FORWARD_OFFSET = 0.5f;

    [SerializeField] private float _holdThreshold = 0.25f;
    [SerializeField] private float _pingCooldown = 0.35f;
    [SerializeField] private LayerMask _pingLayerMask = ~0;
    [SerializeField] private float _raycastDistance = 500f;
    [SerializeField] private GameObject _pingPrefab;

    private float _holdTimer;
    private float _lastPingTime;
    private bool _isHoldingPing;
    private bool _radialMenuOpen;
    private PingType _selectedPingType = PingType.Normal;

    private Camera _playerCamera;
    private PlayerTeam _playerTeam;
    private NetworkPlayerController _playerController;

    public bool IsRadialMenuOpen => _radialMenuOpen;

    private void Awake()
    {
        _playerTeam = GetComponent<PlayerTeam>();
        _playerController = GetComponent<NetworkPlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EnsureCameraReference();
    }

    private void Start()
    {
        EnsureCameraReference();
    }

    private void EnsureCameraReference()
    {
        if (_playerCamera != null) return;

        if (_playerController != null && _playerController.PlayerCamera != null)
        {
            _playerCamera = _playerController.PlayerCamera;
            return;
        }

        _playerCamera = GetComponentInChildren<Camera>(true);
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (IsSpawned && !IsOwner) return;
        if (Mouse.current == null) return;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            if (_isHoldingPing || _radialMenuOpen)
            {
                CancelPingInput();
            }
            return;
        }

        HandlePingInput();
    }

    private void CancelPingInput()
    {
        _isHoldingPing = false;
        _holdTimer = 0f;
        CloseRadialMenu();
    }

    private void HandlePingInput()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            StartPingInput();
        }

        if (!_isHoldingPing) return;

        _holdTimer += Time.unscaledDeltaTime;

        if (!_radialMenuOpen && _holdTimer >= _holdThreshold)
        {
            OpenRadialMenu();
        }

        if (_radialMenuOpen)
        {
            UpdateRadialSelection();
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            FinishPingInput();
        }
    }

    private void StartPingInput()
    {
        if (Time.time < _lastPingTime + _pingCooldown) return;

        _isHoldingPing = true;
        _radialMenuOpen = false;
        _holdTimer = 0f;
        _selectedPingType = PingType.Normal;
    }

    private void OpenRadialMenu()
    {
        _radialMenuOpen = true;

        if (PingWheelUI.Instance != null)
        {
            PingWheelUI.Instance.Open();
        }
        else
        {
            FindAnyObjectByType<PingWheelUI>()?.Open();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _selectedPingType = PingType.Normal;
    }

    private void UpdateRadialSelection()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 direction = mousePosition - screenCenter;

        if (direction.sqrMagnitude < _RADIAL_CENTER_DEADZONE_SQR)
        {
            _selectedPingType = PingType.Normal;
        }
        else if (direction.x < 0f)
        {
            _selectedPingType = PingType.Danger;
        }
        else
        {
            _selectedPingType = PingType.Group;
        }

        if (PingWheelUI.Instance != null)
        {
            PingWheelUI.Instance.UpdateVisuals(_selectedPingType);
        }
        else
        {
            FindAnyObjectByType<PingWheelUI>()?.UpdateVisuals(_selectedPingType);
        }
    }

    private void FinishPingInput()
    {
        if (!_isHoldingPing) return;

        bool wasRadialMenuOpen = _radialMenuOpen;

        _isHoldingPing = false;
        _holdTimer = 0f;

        CloseRadialMenu();

        if (Time.time < _lastPingTime + _pingCooldown) return;

        if (wasRadialMenuOpen && _selectedPingType == PingType.Normal)
        {
            return;
        }

        if (PerformRaycast(out Vector3 hitPosition, out Vector3 hitNormal, out float distance))
        {
            PingType finalType = wasRadialMenuOpen ? _selectedPingType : PingType.Normal;

            if (IsSpawned)
            {
                Team sendingTeam = _playerTeam != null ? _playerTeam.CurrentTeam.Value : Team.Red;
                SendPingServerRpc(hitPosition, hitNormal, finalType, distance, sendingTeam);
            }
            else
            {
                SpawnPingLocally(hitPosition, hitNormal, finalType, distance);
            }

            _lastPingTime = Time.time;
        }
    }

    private void CloseRadialMenu()
    {
        if (PingWheelUI.Instance != null)
        {
            PingWheelUI.Instance.Close();
        }
        else
        {
            FindAnyObjectByType<PingWheelUI>()?.Close();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _radialMenuOpen = false;
    }

    private bool PerformRaycast(out Vector3 hitPoint, out Vector3 hitNormal, out float distance)
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.up;
        distance = 0f;

        EnsureCameraReference();
        if (_playerCamera == null) return false;

        Vector3 rayOrigin = _playerCamera.transform.position + (_playerCamera.transform.forward * _RAY_FORWARD_OFFSET);
        Vector3 rayDirection = _playerCamera.transform.forward;

        int playerLayerMask = 1 << gameObject.layer;
        int activeLayerMask = _pingLayerMask.value == 0 ? ~playerLayerMask : (_pingLayerMask.value & ~playerLayerMask);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, _raycastDistance, activeLayerMask, QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Transform myRoot = transform.root;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.transform.root == myRoot)
                {
                    continue;
                }

                hitPoint = hit.point;
                hitNormal = hit.normal;
                distance = Vector3.Distance(_playerCamera.transform.position, hit.point);
                return true;
            }
        }

        return false;
    }

    [Rpc(SendTo.Server)]
    private void SendPingServerRpc(Vector3 position, Vector3 normal, PingType pingType, float distance, Team team)
    {
        SpawnPingClientRpc(position, normal, pingType, distance, team);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnPingClientRpc(Vector3 position, Vector3 normal, PingType pingType, float distance, Team team)
    {
        if (_playerTeam != null && _playerTeam.CurrentTeam.Value != team) return;

        SpawnPingLocally(position, normal, pingType, distance);
    }

    private void SpawnPingLocally(Vector3 position, Vector3 normal, PingType pingType, float distance)
    {
        if (_pingPrefab == null) return;

        EnsureCameraReference();

        GameObject pingObject = Instantiate(_pingPrefab, position + normal * _NORMAL_PING_OFFSET, Quaternion.identity);
        PingBillboard billboard = pingObject.GetComponent<PingBillboard>();
        if (billboard != null)
        {
            billboard.Initialize(pingType, distance, _playerCamera);
        }
    }
}