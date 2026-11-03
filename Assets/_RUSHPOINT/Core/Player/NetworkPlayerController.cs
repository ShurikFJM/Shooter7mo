using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController), typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    private const float _GRAVITY = -19.62f;
    private const float _HEAD_ROTATION_LIMIT_UP = -89f;
    private const float _HEAD_ROTATION_LIMIT_DOWN = 89f;

    [SerializeField] private float _walkSpeed = 5.5f;
    [SerializeField] private float _slowWalkSpeed = 2.8f;
    [SerializeField] private float _crouchSpeed = 2.4f;
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _crouchCameraDropOffset = 0.55f;
    [SerializeField] private float _crouchControllerHeightOffset = 0.6f;
    [SerializeField] private Transform _cameraAnchor;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private RoleDatabaseSO _roleDatabase;
    [SerializeField] private GameObject _thirdPersonBodyRoot;

    private CharacterController _characterController;
    private WeaponInventory _weaponInventory;
    private NetworkHealth _networkHealth;

    private Vector3 _velocity;
    private float _verticalLookRotation;
    private bool _isCrouching;
    private bool _isSlowWalking;
    private bool _isMoving;
    private RoleDataSO _activeRole;

    private Vector3 _defaultCameraLocalPosition;
    private float _defaultControllerHeight;
    private Vector3 _defaultControllerCenter;

    public RoleDataSO ActiveRole => _activeRole;
    public Camera PlayerCamera => _playerCamera;
    public bool IsMoving => _isMoving;
    public bool IsSlowWalking => _isSlowWalking;
    public bool IsCrouching => _isCrouching;
    public bool IsGrounded => _characterController != null && _characterController.isGrounded;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _weaponInventory = GetComponent<WeaponInventory>();
        _networkHealth = GetComponent<NetworkHealth>();

        if (_playerCamera == null)
        {
            _playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (_cameraAnchor == null && _playerCamera != null)
        {
            _cameraAnchor = _playerCamera.transform;
        }

        if (_cameraAnchor != null)
        {
            _defaultCameraLocalPosition = _cameraAnchor.localPosition;
        }

        if (_characterController != null)
        {
            _defaultControllerHeight = _characterController.height;
            _defaultControllerCenter = _characterController.center;
        }

        if (_audioListener == null && _playerCamera != null)
        {
            _audioListener = _playerCamera.GetComponent<AudioListener>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        bool isLocalPlayer = IsOwner;

        if (_playerCamera != null)
        {
            _playerCamera.enabled = isLocalPlayer;
        }

        if (_audioListener != null)
        {
            _audioListener.enabled = isLocalPlayer;
        }

        if (isLocalPlayer)
        {
            SetCursorLocked(true);
            ConfigureLocalOwnerMeshShadows();

            if (TacticalHUD.Instance != null)
            {
                TacticalHUD.Instance.PlayerHealth = _networkHealth;
                TacticalHUD.Instance.Inventory = _weaponInventory;
            }
        }
    }

    private void ConfigureLocalOwnerMeshShadows()
    {
        GameObject bodyContainer = _thirdPersonBodyRoot != null ? _thirdPersonBodyRoot : gameObject;
        Renderer[] allRenderers = bodyContainer.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] == null) continue;
            if (_weaponInventory != null && allRenderers[i].transform.IsChildOf(_weaponInventory.transform)) continue;

            allRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (_networkHealth != null && !_networkHealth.IsAlive.Value) return;
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;

        HandleRotation();

        bool isRoundActive = RoundManager.Instance == null || RoundManager.Instance.CurrentPhase.Value == RoundPhase.InProgress;
        if (!isRoundActive)
        {
            _isMoving = false;
            _isSlowWalking = false;
            if (_characterController != null && !_characterController.isGrounded)
            {
                _velocity.y += _GRAVITY * Time.deltaTime;
                _characterController.Move(_velocity * Time.deltaTime);
            }
            return;
        }

        HandleCrouch();
        HandleMovement();
        HandleCombatActions();
    }

    private void HandleRotation()
    {
        if (InputManager.Instance == null || _cameraAnchor == null) return;

        Vector2 lookDelta = InputManager.Instance.LookInput;

        transform.Rotate(Vector3.up * lookDelta.x);

        _verticalLookRotation -= lookDelta.y;
        _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, _HEAD_ROTATION_LIMIT_UP, _HEAD_ROTATION_LIMIT_DOWN);
        _cameraAnchor.localRotation = Quaternion.Euler(_verticalLookRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (InputManager.Instance == null || _characterController == null) return;

        bool isGrounded = _characterController.isGrounded;
        if (isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
        }

        Vector2 moveInput = InputManager.Instance.MovementInput;
        Vector3 moveDirection = (transform.right * moveInput.x) + (transform.forward * moveInput.y);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        _isMoving = moveInput.sqrMagnitude > 0.01f;

        bool wantsToSlowWalk = InputManager.Instance.IsWalkPressed;
        _isSlowWalking = wantsToSlowWalk && !_isCrouching && _isMoving;

        float currentSpeed = _walkSpeed;
        if (_isCrouching)
        {
            currentSpeed = _crouchSpeed;
        }
        else if (wantsToSlowWalk)
        {
            currentSpeed = _slowWalkSpeed;
        }

        _characterController.Move(moveDirection * (currentSpeed * Time.deltaTime));

        if (isGrounded && InputManager.Instance.WasJumpTriggered && !_isCrouching)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _GRAVITY);
        }

        _velocity.y += _GRAVITY * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (InputManager.Instance == null || _characterController == null || _cameraAnchor == null) return;

        bool wantsToCrouch = InputManager.Instance.IsCrouchPressed;

        if (wantsToCrouch != _isCrouching)
        {
            _isCrouching = wantsToCrouch;

            float targetHeight = _isCrouching ? (_defaultControllerHeight - _crouchControllerHeightOffset) : _defaultControllerHeight;
            _characterController.height = targetHeight;

            Vector3 targetCenter = _isCrouching
                ? _defaultControllerCenter - new Vector3(0f, _crouchControllerHeightOffset * 0.5f, 0f)
                : _defaultControllerCenter;
            _characterController.center = targetCenter;

            Vector3 targetAnchorPos = _isCrouching
                ? _defaultCameraLocalPosition - new Vector3(0f, _crouchCameraDropOffset, 0f)
                : _defaultCameraLocalPosition;
            _cameraAnchor.localPosition = targetAnchorPos;
        }
    }

    private void HandleCombatActions()
    {
        if (InputManager.Instance == null || _weaponInventory == null) return;

        if (InputManager.Instance.WasSlot1Triggered) _weaponInventory.SwitchSlot(1);
        else if (InputManager.Instance.WasSlot2Triggered) _weaponInventory.SwitchSlot(2);
        else if (InputManager.Instance.WasSlot3Triggered) _weaponInventory.SwitchSlot(3);

        WeaponBase activeWeapon = _weaponInventory.ActiveWeapon;
        if (activeWeapon != null)
        {
            if (activeWeapon.WeaponData != null && activeWeapon.WeaponData.isAutomatic)
            {
                if (InputManager.Instance.IsFirePressed)
                {
                    activeWeapon.Fire();
                }
            }
            else
            {
                if (InputManager.Instance.WasFireTriggered)
                {
                    activeWeapon.Fire();
                }
            }

            if (InputManager.Instance.WasReloadTriggered)
            {
                activeWeapon.Reload();
            }
        }
    }

    public void SetInitialRole(PlayerRoleType roleType)
    {
        RoleDataSO targetRole = ResolveRole(roleType);
        if (targetRole == null) return;

        ApplyRole(targetRole);

        if (IsServer)
        {
            SetRoleClientRpc(roleType);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void SetRoleClientRpc(PlayerRoleType roleType)
    {
        RoleDataSO targetRole = ResolveRole(roleType);
        if (targetRole == null) return;

        ApplyRole(targetRole);
    }

    private RoleDataSO ResolveRole(PlayerRoleType roleType)
    {
        if (_roleDatabase != null)
        {
            RoleDataSO databaseRole = _roleDatabase.GetRole(roleType);
            if (databaseRole != null) return databaseRole;
        }

        RoleDatabaseSO loadedDatabase = Resources.Load<RoleDatabaseSO>("RoleDatabase");
        if (loadedDatabase != null)
        {
            RoleDataSO databaseRole = loadedDatabase.GetRole(roleType);
            if (databaseRole != null) return databaseRole;
        }

        RoleDataSO[] loadedRoles = Resources.LoadAll<RoleDataSO>(string.Empty);
        for (int i = 0; i < loadedRoles.Length; i++)
        {
            if (loadedRoles[i] != null && loadedRoles[i].roleType == roleType)
            {
                return loadedRoles[i];
            }
        }

        return null;
    }

    private void ApplyRole(RoleDataSO roleData)
    {
        _activeRole = roleData;
        _walkSpeed = roleData.walkSpeed;
        _jumpHeight = roleData.jumpForce;

        if (IsServer && _networkHealth != null)
        {
            _networkHealth.SetMaxStatsServer(roleData.maxHealth, roleData.maxArmor);
        }

        ApplyRoleLoadout();
    }

    private void ApplyRoleLoadout()
    {
        if (_activeRole == null || _weaponInventory == null) return;

        _weaponInventory.SetupLoadoutForRole(
            _activeRole.primaryWeaponPrefab,
            _activeRole.secondaryWeaponPrefab,
            _activeRole.tpPrimaryWeaponPrefab,
            _activeRole.tpSecondaryWeaponPrefab
        );

        if (TacticalHUD.Instance != null)
        {
            TacticalHUD.Instance.Inventory = _weaponInventory;
        }
    }

    public void RespawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (_characterController != null) _characterController.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        _verticalLookRotation = 0f;
        if (_cameraAnchor != null) _cameraAnchor.localRotation = Quaternion.identity;
        _velocity = Vector3.zero;
        if (_characterController != null) _characterController.enabled = true;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}