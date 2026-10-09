using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    private const float GRAVITY = -19.62f;
    private const float DEFAULT_WALK_SPEED = 5f;
    private const float DEFAULT_SPRINT_SPEED = 8f;
    private const float DEFAULT_JUMP_FORCE = 1.6f;
    private const float CROUCH_HEIGHT = 1f;
    private const float STANDING_HEIGHT = 2f;
    private const float CROUCH_SPEED_RATIO = 0.5f;
    private const float WALK_SLOW_RATIO = 0.5f;
    private const float CROUCH_TRANSITION_SPEED = 12f;
    private const float MIN_MOVE_MAGNITUDE_SQR = 0.01f;
    private const float CAMERA_PLANE_DISTANCE = 1f;
    private const string LOBBY_CAMERA_TAG = "LobbyCamera";

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _upDownLookLimit = 85f;
    [SerializeField] private RoleDatabaseSO _roleDatabase;
    [SerializeField] private GameObject _firstPersonRoot;
    [SerializeField] private GameObject _thirdPersonRoot;
    [SerializeField] private Transform _hitboxesRoot;
    [SerializeField] private LayerMask _obstacleLayers;

    public NetworkVariable<PlayerRoleType> selectedRole = new NetworkVariable<PlayerRoleType>(
        PlayerRoleType.Assault,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isCrouchedNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public Camera PlayerCamera => _playerCamera;
    public bool IsGrounded => _characterController != null && _characterController.isGrounded;
    public bool IsMoving => _moveInput.sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR;
    public RoleDataSO ActiveRole => _activeRole;

    private RoleDataSO _activeRole;
    private NetworkHealth _networkHealth;
    private PlayerTeam _playerTeam;
    private WeaponInventory _weaponInventory;
    private PlayerMovementAudio _playerMovementAudio;
    private float _verticalVelocity;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isSprinting;
    private bool _isWalkingSlow;
    private bool _isCrouching;
    private bool _jumpRequested;
    private bool _isFirePressed;
    private float _nextFireTime;
    private float _cameraPitch;
    private float _defaultCameraLocalY;
    private bool _hasInitiatedSpectate;
    private PlayerRoleType _lastAppliedRole = (PlayerRoleType)(-1);

    private void Awake()
    {
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_playerCamera == null) _playerCamera = GetComponentInChildren<Camera>(true);
        if (_cameraRoot == null) _cameraRoot = _playerCamera != null ? _playerCamera.transform : transform;
        if (_cameraRoot != null) _defaultCameraLocalY = _cameraRoot.localPosition.y;
        if (_audioListener == null && _playerCamera != null) _audioListener = _playerCamera.GetComponent<AudioListener>();

        _networkHealth = GetComponent<NetworkHealth>();
        _playerTeam = GetComponent<PlayerTeam>();
        _weaponInventory = GetComponentInChildren<WeaponInventory>(true);
        _playerMovementAudio = GetComponent<PlayerMovementAudio>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        selectedRole.OnValueChanged += HandleRoleChanged;
        if (_weaponInventory == null) _weaponInventory = GetComponentInChildren<WeaponInventory>(true);

        ApplyRoleData(selectedRole.Value);
        if (_networkHealth != null) _networkHealth.CurrentHealth.OnValueChanged += HandleHealthChanged;

        Canvas localPlayerCanvas = GetComponentInChildren<Canvas>(true);

        if (IsOwner)
        {
            if (_characterController != null) _characterController.enabled = true;

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
            }

            if (_playerCamera != null) _playerCamera.gameObject.SetActive(true);
            if (_audioListener != null) _audioListener.enabled = true;
            if (_firstPersonRoot != null) _firstPersonRoot.SetActive(true);
            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(false);
            if (localPlayerCanvas != null) localPlayerCanvas.gameObject.SetActive(false);

            if (CursorStateManager.Instance != null)
            {
                CursorStateManager.Instance.OnCursorLockStateChanged += HandleCursorLockStateChanged;
                CursorStateManager.Instance.ForceEvaluateState();
            }
            else
            {
                SetCursorLocked(true);
            }

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && _playerCamera != null)
                {
                    canvas.worldCamera = _playerCamera;
                    canvas.planeDistance = CAMERA_PLANE_DISTANCE;
                }
            }

            TacticalHUD tacticalHud = FindAnyObjectByType<TacticalHUD>();
            if (tacticalHud != null)
            {
                tacticalHud.PlayerHealth = GetComponent<NetworkHealth>();
                tacticalHud.Inventory = _weaponInventory;
            }

            Camera lobbyCamera = GameObject.FindWithTag(LOBBY_CAMERA_TAG)?.GetComponent<Camera>();
            if (lobbyCamera != null) lobbyCamera.gameObject.SetActive(false);
        }
        else
        {
            if (localPlayerCanvas != null) localPlayerCanvas.gameObject.SetActive(false);
            if (_characterController != null) _characterController.enabled = false;

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            if (_playerCamera != null) _playerCamera.gameObject.SetActive(false);
            if (_audioListener != null) _audioListener.enabled = false;
            if (_firstPersonRoot != null) _firstPersonRoot.SetActive(false);
            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);
        }
    }

    public void SetInitialRole(PlayerRoleType roleType)
    {
        if (IsServer)
        {
            selectedRole.Value = roleType;
            ApplyRoleData(roleType);
            SyncRoleClientRpc(roleType);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SyncRoleClientRpc(PlayerRoleType roleType)
    {
        ApplyRoleData(roleType);
    }

    public override void OnNetworkDespawn()
    {
        selectedRole.OnValueChanged -= HandleRoleChanged;
        if (_networkHealth != null) _networkHealth.CurrentHealth.OnValueChanged -= HandleHealthChanged;
        if (IsOwner && CursorStateManager.Instance != null) CursorStateManager.Instance.OnCursorLockStateChanged -= HandleCursorLockStateChanged;
    }

    private void HandleCursorLockStateChanged(bool isLocked)
    {
        if (!isLocked)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _isSprinting = false;
            _isWalkingSlow = false;
            _jumpRequested = false;
            _isFirePressed = false;
        }
    }

    private void HandleHealthChanged(float previousHealth, float currentHealth)
    {
        if (!IsOwner) return;
        if (currentHealth > 0f && _hasInitiatedSpectate) ResetToAliveState();
    }

    private void HandleRoleChanged(PlayerRoleType previousRole, PlayerRoleType currentRole)
    {
        ApplyRoleData(currentRole);
    }

    private void ApplyRoleData(PlayerRoleType roleType)
    {
        if (_roleDatabase == null) return;
        if (_lastAppliedRole == roleType && _activeRole != null) return;

        _activeRole = _roleDatabase.GetRole(roleType);
        if (_activeRole == null) return;

        _lastAppliedRole = roleType;

        if (IsServer)
        {
            NetworkHealth networkHealth = GetComponent<NetworkHealth>();
            if (networkHealth != null) networkHealth.SetMaxStatsServer(_activeRole.maxHealth, _activeRole.maxArmor);
        }

        if (_weaponInventory == null) _weaponInventory = GetComponentInChildren<WeaponInventory>(true);

        if (_weaponInventory != null)
        {
            _weaponInventory.SetupLoadoutForRole(
                _activeRole.primaryWeaponPrefab,
                _activeRole.secondaryWeaponPrefab,
                _activeRole.tpPrimaryWeaponPrefab,
                _activeRole.tpSecondaryWeaponPrefab
            );
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleDeathCheck();
            if (SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating) return;

            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _isFirePressed = false;
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                bool isUiOpen = (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
                                (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen) ||
                                (RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value == RoundPhase.WaitingForPlayers);

                if (!isUiOpen && Cursor.lockState != CursorLockMode.Locked)
                {
                    if (CursorStateManager.Instance != null) CursorStateManager.Instance.ForceEvaluateState();
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            UpdateInputStates();
            HandleCameraRotation();
            HandleMovementExecution();
            HandleWeaponCombatInput();
        }

        HandleCrouchHeightTransition();
    }

    private void HandleWeaponCombatInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value != RoundPhase.InProgress) return;

        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        if (bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing)) return;
        if (_weaponInventory == null) return;

        WeaponBase currentWeapon = _weaponInventory.ActiveWeapon;
        if (currentWeapon == null) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            currentWeapon.Reload();
        }

        bool isLeftMouseButtonPressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool wantsToFire = _isFirePressed || isLeftMouseButtonPressed;

        if (!wantsToFire) return;

        WeaponData weaponData = currentWeapon.WeaponData;
        bool isAutomatic = weaponData != null && weaponData.isAutomatic;

        if (currentWeapon.CanFire())
        {
            if (currentWeapon is HealingPistol healingPistol) healingPistol.PerformHealShot();
            else currentWeapon.Fire();

            if (!isAutomatic) _isFirePressed = false;
        }
    }

    public void OnFire(InputValue value)
    {
        _isFirePressed = value.isPressed;
    }

    public void OnReload(InputValue value)
    {
        if (value.isPressed && _weaponInventory != null && _weaponInventory.ActiveWeapon != null)
        {
            _weaponInventory.ActiveWeapon.Reload();
        }
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        if (!locked)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _isSprinting = false;
            _isWalkingSlow = false;
            _jumpRequested = false;
            _isFirePressed = false;
        }
    }

    private void HandleDeathCheck()
    {
        if (_networkHealth == null) return;

        bool isDead = !_networkHealth.IsAlive.Value || _networkHealth.CurrentHealth.Value <= 0f;

        if (isDead && !_hasInitiatedSpectate)
        {
            _hasInitiatedSpectate = true;

            if (_characterController != null) _characterController.enabled = false;
            if (_firstPersonRoot != null) _firstPersonRoot.SetActive(false);

            if (SpectatorManager.Instance != null && _playerTeam != null && _playerCamera != null)
            {
                SpectatorManager.Instance.StartSpectating(_playerTeam.CurrentTeam.Value, _playerCamera, transform, _defaultCameraLocalY);
            }
        }
    }

    private void ResetToAliveState()
    {
        _hasInitiatedSpectate = false;
        if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();

        if (_cameraRoot != null)
        {
            _cameraRoot.localPosition = new Vector3(0f, _defaultCameraLocalY, 0f);
            _cameraRoot.localRotation = Quaternion.identity;
        }

        _cameraPitch = 0f;
        if (_firstPersonRoot != null) _firstPersonRoot.SetActive(true);
        if (_characterController != null) _characterController.enabled = true;
    }

    private void UpdateInputStates()
    {
        bool isLockedPhase = RoundManager.Instance != null &&
            (RoundManager.Instance.CurrentPhase.Value == RoundPhase.WaitingForPlayers ||
             RoundManager.Instance.CurrentPhase.Value == RoundPhase.Warmup);

        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        bool isInteractingBomb = bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing);

        if (isLockedPhase || isInteractingBomb)
        {
            _moveInput = Vector2.zero;
            _isSprinting = false;
            _isWalkingSlow = false;
            _jumpRequested = false;
            return;
        }

        bool crouchPressed = false;
        bool walkSlowPressed = false;
        bool sprintPressed = false;

        if (Keyboard.current != null)
        {
            crouchPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
            walkSlowPressed = Keyboard.current.leftShiftKey.isPressed;
            sprintPressed = Keyboard.current.leftShiftKey.isPressed && !_isWalkingSlow;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonEast.isPressed || Gamepad.current.rightStickButton.isPressed) crouchPressed = true;
            if (Gamepad.current.leftStickButton.isPressed) sprintPressed = true;
        }

        _isWalkingSlow = walkSlowPressed;
        _isSprinting = sprintPressed;

        bool targetCrouch = _isCrouching;
        if (crouchPressed) targetCrouch = true;
        else if (!HasCeilingObstacle()) targetCrouch = false;

        if (targetCrouch != _isCrouching)
        {
            _isCrouching = targetCrouch;
            SetCrouchStateServerRpc(_isCrouching);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetCrouchStateServerRpc(bool crouching)
    {
        isCrouchedNet.Value = crouching;
    }

    public void OnMove(InputValue value)
    {
        bool isLockedPhase = RoundManager.Instance != null &&
            (RoundManager.Instance.CurrentPhase.Value == RoundPhase.WaitingForPlayers ||
             RoundManager.Instance.CurrentPhase.Value == RoundPhase.Warmup);

        if (isLockedPhase || (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused))
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            _lookInput = Vector2.zero;
            return;
        }

        _lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        bool isLockedPhase = RoundManager.Instance != null &&
            (RoundManager.Instance.CurrentPhase.Value == RoundPhase.WaitingForPlayers ||
             RoundManager.Instance.CurrentPhase.Value == RoundPhase.Warmup);

        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        bool isInteractingBomb = bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing);

        if (isLockedPhase || isInteractingBomb || (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)) return;

        if (value.isPressed && IsGrounded && !_isCrouching) _jumpRequested = true;
    }

    private bool HasCeilingObstacle()
    {
        if (_obstacleLayers.value == 0 || _characterController == null) return false;

        float radius = _characterController.radius * 0.7f;
        Vector3 pointBottom = transform.position + Vector3.up * (CROUCH_HEIGHT + 0.1f);
        Vector3 pointTop = transform.position + Vector3.up * (STANDING_HEIGHT - radius);

        if (pointTop.y <= pointBottom.y) return false;

        Collider[] hits = Physics.OverlapCapsule(pointBottom, pointTop, radius, _obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject != gameObject && !hits[i].transform.IsChildOf(transform)) return true;
        }
        return false;
    }

    private void HandleCameraRotation()
    {
        if (_cameraRoot == null || Cursor.lockState != CursorLockMode.Locked) return;

        float mouseYaw = _lookInput.x * _mouseSensitivity;
        transform.Rotate(Vector3.up * mouseYaw);

        _cameraPitch -= _lookInput.y * _mouseSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -_upDownLookLimit, _upDownLookLimit);
        _cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    private void HandleCrouchHeightTransition()
    {
        bool crouched = IsOwner ? _isCrouching : isCrouchedNet.Value;

        if (_characterController != null && IsOwner)
        {
            float targetHeight = crouched ? CROUCH_HEIGHT : STANDING_HEIGHT;
            _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * CROUCH_TRANSITION_SPEED);
            _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);
        }

        if (_cameraRoot != null && IsOwner && !_hasInitiatedSpectate)
        {
            float targetCameraY = crouched ? _defaultCameraLocalY * 0.55f : _defaultCameraLocalY;
            Vector3 targetCameraPosition = _cameraRoot.localPosition;
            targetCameraPosition.y = Mathf.Lerp(targetCameraPosition.y, targetCameraY, Time.deltaTime * CROUCH_TRANSITION_SPEED);
            _cameraRoot.localPosition = targetCameraPosition;
        }

        float targetScaleY = crouched ? 0.5f : 1f;

        if (_thirdPersonRoot != null)
        {
            Vector3 visualScale = _thirdPersonRoot.transform.localScale;
            visualScale.y = Mathf.Lerp(visualScale.y, targetScaleY, Time.deltaTime * CROUCH_TRANSITION_SPEED);
            _thirdPersonRoot.transform.localScale = visualScale;
        }

        if (_hitboxesRoot != null)
        {
            Vector3 hitboxScale = _hitboxesRoot.localScale;
            hitboxScale.y = Mathf.Lerp(hitboxScale.y, targetScaleY, Time.deltaTime * CROUCH_TRANSITION_SPEED);
            _hitboxesRoot.localScale = hitboxScale;
        }
    }

    private void HandleMovementExecution()
    {
        if (_characterController == null) return;

        bool isLockedPhase = RoundManager.Instance != null &&
            (RoundManager.Instance.CurrentPhase.Value == RoundPhase.WaitingForPlayers ||
             RoundManager.Instance.CurrentPhase.Value == RoundPhase.Warmup);

        if (isLockedPhase)
        {
            if (_characterController.isGrounded) _verticalVelocity = -2f;
            else _verticalVelocity += GRAVITY * Time.deltaTime;

            _characterController.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
            return;
        }

        float baseSpeed = _activeRole != null ? _activeRole.walkSpeed : DEFAULT_WALK_SPEED;
        float currentSpeed = baseSpeed;

        if (_isCrouching) currentSpeed *= CROUCH_SPEED_RATIO;
        else if (_isWalkingSlow) currentSpeed *= WALK_SLOW_RATIO;
        else if (_isSprinting) currentSpeed = _activeRole != null ? _activeRole.sprintSpeed : DEFAULT_SPRINT_SPEED;

        Vector3 moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        if (_characterController.isGrounded)
        {
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;
            if (_jumpRequested)
            {
                float jumpForce = _activeRole != null ? _activeRole.jumpForce : DEFAULT_JUMP_FORCE;
                _verticalVelocity = Mathf.Sqrt(jumpForce * -2f * GRAVITY);
                _jumpRequested = false;
                if (_playerMovementAudio != null) _playerMovementAudio.OnPlayerJumped();
            }
        }
        else
        {
            _verticalVelocity += GRAVITY * Time.deltaTime;
        }

        Vector3 finalVelocity = (moveDirection * currentSpeed) + (Vector3.up * _verticalVelocity);
        _characterController.Move(finalVelocity * Time.deltaTime);
    }

    public void RespawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (_characterController != null) _characterController.enabled = false;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (_cameraRoot != null)
        {
            _cameraRoot.localPosition = new Vector3(0f, _defaultCameraLocalY, 0f);
            _cameraRoot.localRotation = Quaternion.identity;
        }

        _cameraPitch = 0f;
        _verticalVelocity = 0f;
        _moveInput = Vector2.zero;
        _hasInitiatedSpectate = false;

        if (IsOwner)
        {
            if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
            if (_firstPersonRoot != null) _firstPersonRoot.SetActive(true);
            if (_playerCamera != null) _playerCamera.gameObject.SetActive(true);
            if (_characterController != null) _characterController.enabled = true;
        }
        else
        {
            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);
        }
    }
}