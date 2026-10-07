using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerPingSystem : NetworkBehaviour
{
    public enum PingType
    {
        Normal = 0,
        Danger = 1,
        Group = 2
    }

    [SerializeField] private float _holdThreshold = 0.25f;
    [SerializeField] private float _pingCooldown = 0.35f;
    [SerializeField] private LayerMask _pingLayerMask = ~0;
    [SerializeField] private float _raycastDistance = 500f;
    [SerializeField] private GameObject _radialMenu;
    [SerializeField] private Image _dangerOption;
    [SerializeField] private Image _groupOption;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Behaviour _cameraLookController;
    [SerializeField] private GameObject _pingPrefab;

    private float _holdTimer;
    private float _lastPingTime;
    private bool _isHoldingPing;
    private bool _radialMenuOpen;
    private PingType _selectedPingType = PingType.Normal;

    private void Awake()
    {
        if (_radialMenu != null)
        {
            _radialMenu.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            EnsureCameraReference();
        }
    }

    private void EnsureCameraReference()
    {
        if (_playerCamera == null)
        {
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (Mouse.current == null) return;

        HandlePingInput();
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

        if (_radialMenu != null)
        {
            _radialMenu.SetActive(true);
        }

        if (_cameraLookController != null)
        {
            _cameraLookController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _selectedPingType = PingType.Normal;
        UpdateRadialVisuals(PingType.Normal);
    }

    private void UpdateRadialSelection()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 direction = mousePosition - screenCenter;

        if (direction.sqrMagnitude < 2500f)
        {
            _selectedPingType = PingType.Normal;
            UpdateRadialVisuals(PingType.Normal);
            return;
        }

        if (direction.x < 0f)
        {
            _selectedPingType = PingType.Danger;
        }
        else
        {
            _selectedPingType = PingType.Group;
        }

        UpdateRadialVisuals(_selectedPingType);
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
            Debug.Log("[PING SYSTEM] Selección cancelada en el centro del menú radial.");
            return;
        }

        if (PerformRaycast(out Vector3 hitPosition, out Vector3 hitNormal, out float distance, out string hitName))
        {
            PingType finalType = wasRadialMenuOpen ? _selectedPingType : PingType.Normal;

            Debug.Log($"<color=green>[PING OK]</color> Tipo: <b>{finalType}</b> | Objeto: <b>{hitName}</b> | Distancia: <b>{distance:F1} metros</b> | Posición: {hitPosition}");

            SendPingServerRpc(hitPosition, hitNormal, finalType, distance);
            _lastPingTime = Time.time;
        }
        else
        {
            Debug.LogWarning("[PING WARN] Raycast no impactó con ninguna superficie o capa válida.");
        }
    }

    private void CloseRadialMenu()
    {
        if (_radialMenu != null)
        {
            _radialMenu.SetActive(false);
        }

        if (_cameraLookController != null)
        {
            _cameraLookController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _radialMenuOpen = false;
    }

    private bool PerformRaycast(out Vector3 hitPoint, out Vector3 hitNormal, out float distance, out string hitObjectName)
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.up;
        distance = 0f;
        hitObjectName = "Ninguno";

        EnsureCameraReference();
        if (_playerCamera == null)
        {
            Debug.LogError("[PING ERROR] No se encontró referencia a la cámara del jugador.");
            return false;
        }
        Ray ray = new Ray(_playerCamera.transform.position + _playerCamera.transform.forward * 0.2f, _playerCamera.transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, _raycastDistance, _pingLayerMask, QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                {
                    continue;
                }

                hitPoint = hit.point;
                hitNormal = hit.normal;
                distance = Vector3.Distance(_playerCamera.transform.position, hit.point);
                hitObjectName = hit.collider.name;

                Debug.DrawLine(_playerCamera.transform.position, hit.point, Color.cyan, 3f);

                return true;
            }
        }

        return false;
    }

    private void UpdateRadialVisuals(PingType selectedType)
    {
        if (_dangerOption != null)
        {
            _dangerOption.color = (selectedType == PingType.Danger) ? Color.red : new Color(1f, 1f, 1f, 0.35f);
        }

        if (_groupOption != null)
        {
            _groupOption.color = (selectedType == PingType.Group) ? Color.cyan : new Color(1f, 1f, 1f, 0.35f);
        }
    }

    [ServerRpc]
    private void SendPingServerRpc(Vector3 position, Vector3 normal, PingType pingType, float distance)
    {
        SpawnPingClientRpc(position, normal, pingType, distance);
    }

    [ClientRpc]
    private void SpawnPingClientRpc(Vector3 position, Vector3 normal, PingType pingType, float distance)
    {
        if (_pingPrefab == null)
        {
            Debug.LogError("[PING ERROR] No se ha asignado el _pingPrefab en el Inspector.");
            return;
        }

        EnsureCameraReference();

        GameObject pingObject = Instantiate(_pingPrefab, position + normal * 0.2f, Quaternion.identity);

        PingBillboard billboard = pingObject.GetComponent<PingBillboard>();
        if (billboard != null)
        {
            billboard.Initialize(pingType, distance, _playerCamera);
        }
        else
        {
            Debug.LogError("[PING ERROR] El prefab instanciado no contiene el componente PingBillboard.");
        }
    }
}