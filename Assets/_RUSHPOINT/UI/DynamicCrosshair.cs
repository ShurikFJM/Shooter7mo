using System.Reflection;
using Unity.Netcode;
using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    private const float MIN_VALID_SCALE_MULTIPLIER = 650f;
    private const float MOVEMENT_SPEED_THRESHOLD = 0.15f;
    private const float DEFAULT_MOVEMENT_BASE_ADDITION = 0.01f;
    private const float DEFAULT_AIR_BASE_ADDITION = 0.02f;
    private const float MAX_TOTAL_SPREAD = 0.1f;
    private const float DEFAULT_CAMERA_FOV = 60f;
    private const float REFERENCE_CANVAS_HALF_HEIGHT = 540f;
    private const float RESTING_SPREAD_CAP = 0.012f;

    [SerializeField] private RectTransform _topTick;
    [SerializeField] private RectTransform _bottomTick;
    [SerializeField] private RectTransform _leftTick;
    [SerializeField] private RectTransform _rightTick;
    [SerializeField] private float _baseGap = 12f;
    [SerializeField] private float _gapScaleMultiplier = 900f;
    [SerializeField] private float _maxGap = 160f;
    [SerializeField] private float _expandSpeed = 35f;
    [SerializeField] private float _contractSpeed = 22f;
    [SerializeField] private bool _useCameraProjection = true;
    [SerializeField] private WeaponInventory _inventory;

    private NetworkPlayerController _playerController;
    private CharacterController _characterController;
    private Camera _playerCamera;
    private Canvas _parentCanvas;
    private RectTransform _canvasRectTransform;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPosition;
    private float _currentGap;
    private WeaponBase _lastActiveWeapon;

    private static readonly FieldInfo _weaponPlayerControllerField = typeof(WeaponBase).GetField(
        "_playerController",
        BindingFlags.NonPublic | BindingFlags.Instance
    );

    private static readonly FieldInfo _weaponFirePointField = typeof(WeaponBase).GetField(
        "_firePoint",
        BindingFlags.NonPublic | BindingFlags.Instance
    );

    public WeaponInventory Inventory
    {
        get => _inventory;
        set
        {
            _inventory = value;
            CachePlayerComponents();
        }
    }

    private void Awake()
    {
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
        {
            _canvasRectTransform = _parentCanvas.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        _currentGap = _baseGap;
        ApplyCrosshairGap(_currentGap);
    }

    private void Update()
    {
        if (_inventory == null || _playerController == null)
        {
            TryBindLocalPlayer();
            if (_inventory == null) return;
        }

        WeaponBase activeWeapon = _inventory.ActiveWeapon;
        if (activeWeapon == null)
        {
            UpdateGapInterpolation(_baseGap);
            return;
        }

        if (activeWeapon != _lastActiveWeapon)
        {
            _lastActiveWeapon = activeWeapon;
            EnsureWeaponDependencies(activeWeapon);
        }

        float synchronizedSpread = CalculateSynchronizedSpread(activeWeapon);
        float spreadInPixels = ConvertSpreadToPixels(synchronizedSpread);
        float targetGap = _baseGap + spreadInPixels;

        if (activeWeapon.IsReloading)
        {
            targetGap = _baseGap;
        }

        targetGap = Mathf.Clamp(targetGap, _baseGap, _maxGap);
        UpdateGapInterpolation(targetGap);
    }

    private void EnsureWeaponDependencies(WeaponBase weapon)
    {
        if (weapon == null || _playerController == null) return;

        if (_weaponPlayerControllerField != null)
        {
            object currentController = _weaponPlayerControllerField.GetValue(weapon);
            if (currentController == null)
            {
                _weaponPlayerControllerField.SetValue(weapon, _playerController);
            }
        }

        if (_weaponFirePointField != null && _playerCamera != null)
        {
            object currentFirePoint = _weaponFirePointField.GetValue(weapon);
            if (currentFirePoint == null)
            {
                _weaponFirePointField.SetValue(weapon, _playerCamera.transform);
            }
        }
    }

    private float CalculateSynchronizedSpread(WeaponBase activeWeapon)
    {
        bool isMoving = IsPlayerCurrentlyMoving();
        bool isGrounded = IsPlayerCurrentlyGrounded();

        WeaponData data = activeWeapon.WeaponData;

        if (!isMoving && isGrounded)
        {
            float restingBaseSpread = (data != null) ? Mathf.Min(data.baseSpread, RESTING_SPREAD_CAP) : 0f;
            float shotPenalty = 0f;

            float reportedSpread = activeWeapon.GetCurrentSpread();
            if (reportedSpread > 0.001f)
            {
                shotPenalty = Mathf.Min(reportedSpread, MAX_TOTAL_SPREAD);
            }

            return Mathf.Clamp(restingBaseSpread + shotPenalty, 0f, MAX_TOTAL_SPREAD);
        }

        float movementCalculatedSpread = 0f;

        if (data != null)
        {
            float sanitizedBase = Mathf.Min(data.baseSpread, RESTING_SPREAD_CAP);

            if (!isGrounded)
            {
                float clampedAirMult = Mathf.Clamp(data.airSpreadMultiplier, 1f, 4.5f);
                movementCalculatedSpread = (sanitizedBase + DEFAULT_AIR_BASE_ADDITION) * clampedAirMult;
            }
            else if (isMoving)
            {
                float clampedMoveMult = Mathf.Clamp(data.movementSpreadMultiplier, 1f, 2.2f);
                movementCalculatedSpread = (sanitizedBase + DEFAULT_MOVEMENT_BASE_ADDITION) * clampedMoveMult;
            }
        }

        float actualWeaponSpread = activeWeapon.GetCurrentSpread();
        float combinedSpread = Mathf.Max(actualWeaponSpread, movementCalculatedSpread);

        return Mathf.Clamp(combinedSpread, 0f, MAX_TOTAL_SPREAD);
    }

    private bool IsPlayerCurrentlyMoving()
    {
        if (_playerController != null && _playerController.IsMoving)
        {
            UpdateLastPosition();
            return true;
        }

        if (_characterController != null)
        {
            Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            if (horizontalVelocity.magnitude > MOVEMENT_SPEED_THRESHOLD)
            {
                UpdateLastPosition();
                return true;
            }
        }

        if (_playerController != null)
        {
            Vector3 currentPosition = _playerController.transform.position;
            if (!_hasLastPosition)
            {
                _lastPlayerPosition = currentPosition;
                _hasLastPosition = true;
                return false;
            }

            Vector3 horizontalDelta = currentPosition - _lastPlayerPosition;
            horizontalDelta.y = 0f;
            float speed = Time.deltaTime > 0.0001f ? horizontalDelta.magnitude / Time.deltaTime : 0f;
            _lastPlayerPosition = currentPosition;

            return speed > MOVEMENT_SPEED_THRESHOLD;
        }

        return false;
    }

    private bool IsPlayerCurrentlyGrounded()
    {
        if (_playerController != null)
        {
            return _playerController.IsGrounded;
        }

        if (_characterController != null)
        {
            return _characterController.isGrounded;
        }

        return true;
    }

    private void UpdateLastPosition()
    {
        if (_playerController != null)
        {
            _lastPlayerPosition = _playerController.transform.position;
            _hasLastPosition = true;
        }
    }

    private float ConvertSpreadToPixels(float spread)
    {
        if (spread <= 0.0001f) return 0f;

        if (_useCameraProjection)
        {
            float fov = _playerCamera != null ? _playerCamera.fieldOfView : DEFAULT_CAMERA_FOV;
            float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
            float tanHalfFov = Mathf.Tan(halfFovRad);

            if (tanHalfFov > 0.001f)
            {
                float canvasHalfHeight = _canvasRectTransform != null && _canvasRectTransform.rect.height > 0f
                    ? _canvasRectTransform.rect.height * 0.5f
                    : REFERENCE_CANVAS_HALF_HEIGHT;

                return (spread / tanHalfFov) * canvasHalfHeight;
            }
        }

        float effectiveMultiplier = Mathf.Max(_gapScaleMultiplier, MIN_VALID_SCALE_MULTIPLIER);
        return spread * effectiveMultiplier;
    }

    private void UpdateGapInterpolation(float targetGap)
    {
        float lerpSpeed = (targetGap > _currentGap) ? _expandSpeed : _contractSpeed;
        _currentGap = Mathf.Lerp(_currentGap, targetGap, Time.deltaTime * lerpSpeed);
        ApplyCrosshairGap(_currentGap);
    }

    private void ApplyCrosshairGap(float gap)
    {
        if (_topTick != null) _topTick.anchoredPosition = new Vector2(0f, gap);
        if (_bottomTick != null) _bottomTick.anchoredPosition = new Vector2(0f, -gap);
        if (_leftTick != null) _leftTick.anchoredPosition = new Vector2(-gap, 0f);
        if (_rightTick != null) _rightTick.anchoredPosition = new Vector2(gap, 0f);
    }

    private void TryBindLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            NetworkObject localPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            _inventory = localPlayerObj.GetComponentInChildren<WeaponInventory>(true);
            CachePlayerComponents();
            return;
        }

        WeaponInventory[] inventories = FindObjectsByType<WeaponInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < inventories.Length; i++)
        {
            if (inventories[i].IsOwner || !NetworkManager.Singleton.IsListening)
            {
                _inventory = inventories[i];
                CachePlayerComponents();
                return;
            }
        }
    }

    private void CachePlayerComponents()
    {
        if (_inventory == null) return;

        _playerController = _inventory.GetComponentInParent<NetworkPlayerController>();
        if (_playerController == null)
        {
            _playerController = _inventory.GetComponent<NetworkPlayerController>();
        }

        if (_playerController != null)
        {
            _characterController = _playerController.GetComponent<CharacterController>();
            _playerCamera = _playerController.PlayerCamera;
            _lastPlayerPosition = _playerController.transform.position;
            _hasLastPosition = true;
        }

        if (_playerCamera == null)
        {
            _playerCamera = _inventory.GetComponentInChildren<Camera>(true);
        }
    }
}