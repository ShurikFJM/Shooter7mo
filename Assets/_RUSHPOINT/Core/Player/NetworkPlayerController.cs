using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    private const float GRAVITY = -19.62f;
    private const float DEFAULT_WALK_SPEED = 5.5f;
    private const float DEFAULT_JUMP_FORCE = 1.6f;
    private const float CROUCH_HEIGHT = 1f;
    private const float STANDING_HEIGHT = 2f;
    private const float CROUCH_SPEED_RATIO = 0.5f;
    private const float WALK_SLOW_RATIO = 0.5f;
    private const float CROUCH_TRANSITION_SPEED = 12f;
    private const float MIN_MOVE_MAGNITUDE_SQR = 0.005f;
    private const float CAMERA_PLANE_DISTANCE = 1f;
    private const string LOBBY_CAMERA_TAG = "LobbyCamera";
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    private bool _isStunned;
    private float _stunSlowRatio = 1.0f;
    private float _stunSensRatio = 1.0f;
    private Coroutine _stunCoroutine;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _upDownLookLimit = 85f;
    [SerializeField] private RoleDatabaseSO _roleDatabase;
    [SerializeField] private GameObject _firstPersonRoot;
    [SerializeField] private GameObject _thirdPersonRoot;

    [Header("Modelos de Soldados (Auto-detectables incluso con _Variant)")]
    [SerializeField] private GameObject _infantryModel;
    [SerializeField] private GameObject _gunnerModel;
    [SerializeField] private GameObject _medicModel;

    [Header("Rol por defecto para Pruebas directas")]
    [SerializeField] private PlayerRoleType _defaultTestingRole = PlayerRoleType.Tank;

    [SerializeField] private Transform _hitboxesRoot;
    [SerializeField] private LayerMask _obstacleLayers;

    [Header("Animación del Soldado")]
    private Animator _activeAnimator;

    public NetworkVariable<PlayerRoleType> selectedRole = new NetworkVariable<PlayerRoleType>(
        PlayerRoleType.Tank,
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
    public bool IsMoving => _currentMoveInput.sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR;
    public bool IsWalkingSlow => _isWalkingSlow;
    public bool IsSlowWalking => _isWalkingSlow;
    public bool IsCrouching => _isCrouching;
    public RoleDataSO ActiveRole => _activeRole;

    private RoleDataSO _activeRole;
    private NetworkHealth _networkHealth;
    private PlayerTeam _playerTeam;
    private WeaponInventory _weaponInventory;
    private PlayerMovementAudio _playerMovementAudio;
    private Transform _spineBone;

    private float _verticalVelocity;
    private Vector2 _eventMoveInput;
    private Vector2 _eventLookInput;
    private Vector2 _currentMoveInput;
    private Vector2 _currentLookRotation;
    private bool _isWalkingSlow;
    private bool _isCrouching;
    private bool _jumpRequested;
    private bool _isFirePressed;
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

        EnsureRoleDatabaseReference();
        EnsureModelReferences();
        LocateSpineBone();
    }

    private void EnsureRoleDatabaseReference()
    {
        if (_roleDatabase != null) return;

        if (RoleSelectScreenUI.instance != null && RoleSelectScreenUI.instance.RoleDatabase != null)
        {
            _roleDatabase = RoleSelectScreenUI.instance.RoleDatabase;
            return;
        }

        RoleDatabaseSO[] found = Resources.FindObjectsOfTypeAll<RoleDatabaseSO>();
        if (found != null && found.Length > 0)
        {
            _roleDatabase = found[0];
        }
    }

    private void EnsureModelReferences()
    {
        Transform tpRoot = _thirdPersonRoot != null ? _thirdPersonRoot.transform : null;
        if (tpRoot == null)
        {
            Transform foundTp = transform.Find("ThirdPersonModel");
            if (foundTp != null)
            {
                _thirdPersonRoot = foundTp.gameObject;
                tpRoot = foundTp;
            }
            else
            {
                tpRoot = transform;
            }
        }

        Transform[] allChildren = tpRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            string childName = allChildren[i].name;
            if (_gunnerModel == null && childName.IndexOf("gunner", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _gunnerModel = allChildren[i].gameObject;
            }
            else if (_medicModel == null && childName.IndexOf("medic", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _medicModel = allChildren[i].gameObject;
            }
            else if (_infantryModel == null && childName.IndexOf("infantry", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _infantryModel = allChildren[i].gameObject;
            }
        }
    }

    private void LocateSpineBone()
    {
        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null && anim.isHuman)
        {
            _spineBone = anim.GetBoneTransform(HumanBodyBones.Spine);
            if (_spineBone == null) _spineBone = anim.GetBoneTransform(HumanBodyBones.Chest);
        }

        if (_spineBone == null)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.IndexOf("Spine", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _spineBone = transforms[i];
                    break;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EnsureRoleDatabaseReference();
        EnsureModelReferences();
        LocateSpineBone();

        selectedRole.OnValueChanged += HandleRoleChanged;
        if (_weaponInventory == null) _weaponInventory = GetComponentInChildren<WeaponInventory>(true);
        if (_networkHealth != null) _networkHealth.CurrentHealth.OnValueChanged += HandleHealthChanged;

        Canvas localPlayerCanvas = GetComponentInChildren<Canvas>(true);

        if (IsOwner)
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.EnableAllGameplayActions();
            }

            if (_characterController != null) _characterController.enabled = true;

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
            }

            if (_playerCamera != null) _playerCamera.gameObject.SetActive(true);
            if (_audioListener != null) _audioListener.enabled = true;
            if (_firstPersonRoot != null) _firstPersonRoot.SetActive(false);

            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);
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

            PlayerRoleType intendedRole = _defaultTestingRole;
            if (RoleSelectScreenUI.instance != null && RoleSelectScreenUI.instance.hasLockedIn)
            {
                intendedRole = RoleSelectScreenUI.instance.SelectedRole;
            }
            else if (CustomLobbyManager.SavedRoles != null && CustomLobbyManager.SavedRoles.TryGetValue(NetworkManager.Singleton.LocalClientId, out PlayerRoleType savedRole))
            {
                intendedRole = savedRole;
            }
            else if (selectedRole.Value != PlayerRoleType.Assault)
            {
                intendedRole = selectedRole.Value;
            }

            RequestSelectRoleServerRpc(intendedRole);
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

            ApplyRoleData(selectedRole.Value);
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

    [Rpc(SendTo.Server)]
    public void RequestSelectRoleServerRpc(PlayerRoleType roleType, RpcParams rpcParams = default)
    {
        selectedRole.Value = roleType;
        ApplyRoleData(roleType);
        SyncRoleClientRpc(roleType);

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (CustomLobbyManager.SavedRoles != null)
        {
            CustomLobbyManager.SavedRoles[senderClientId] = roleType;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SyncRoleClientRpc(PlayerRoleType roleType)
    {
        ApplyRoleData(roleType);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SyncRoundStateClientRpc(
        int roundTime,
        int redScore,
        int blueScore,
        int roundNumber,
        byte phase,
        byte winner)
    {
        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Dismiss();
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.ApplyClientSyncedState(
                roundTime,
                redScore,
                blueScore,
                roundNumber,
                (RoundPhase)phase,
                (Team)winner
            );
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TeleportAndResetClientRpc(Vector3 targetPosition, Quaternion targetRotation = default, ulong targetClientId = 0)
    {
        if (targetClientId != 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId != targetClientId)
        {
            return;
        }

        if (targetRotation == default) targetRotation = Quaternion.identity;

        if (_characterController != null) _characterController.enabled = false;

        RespawnPlayer(targetPosition, targetRotation);

        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(targetPosition, targetRotation, transform.localScale);
        }

        if (_characterController != null && IsOwner) _characterController.enabled = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ReturnToMainMenuClientRpc()
    {
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
        yield return null;
    }

    public void SetFirstPersonVisibility(bool visible)
    {
        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(visible);
        if (_firstPersonRoot != null) _firstPersonRoot.SetActive(visible);
        if (_weaponInventory != null && _weaponInventory.ActiveWeapon != null)
        {
            _weaponInventory.ActiveWeapon.gameObject.SetActive(visible);
        }
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
            _currentMoveInput = Vector2.zero;
            _currentLookRotation = Vector2.zero;
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
        EnsureRoleDatabaseReference();
        EnsureModelReferences();

        if (_roleDatabase == null) return;

        _activeRole = _roleDatabase.GetRole(roleType);
        if (_activeRole == null) return;

        _lastAppliedRole = roleType;

        if (IsServer)
        {
            NetworkHealth networkHealth = GetComponent<NetworkHealth>();
            if (networkHealth != null)
            {
                networkHealth.SetMaxStatsServer(_activeRole.maxHealth, _activeRole.maxArmor);
            }
        }

        if (_infantryModel != null) _infantryModel.SetActive(roleType == PlayerRoleType.Assault);
        if (_gunnerModel != null) _gunnerModel.SetActive(roleType == PlayerRoleType.Tank);
        if (_medicModel != null) _medicModel.SetActive(roleType == PlayerRoleType.Medic);

        bool anyActive = (_infantryModel != null && _infantryModel.activeSelf) ||
                         (_gunnerModel != null && _gunnerModel.activeSelf) ||
                         (_medicModel != null && _medicModel.activeSelf);
        if (!anyActive && _gunnerModel != null)
        {
            _gunnerModel.SetActive(true);
        }

        _activeAnimator = null;
        LocateSpineBone();

        PlayerTeamVisualizer visualizer = GetComponent<PlayerTeamVisualizer>();
        if (visualizer != null)
        {
            visualizer.RefreshRenderers();
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

            _weaponInventory.SwitchSlot(1);

            TacticalHUD tacticalHud = FindAnyObjectByType<TacticalHUD>();
            if (tacticalHud != null)
            {
                tacticalHud.Inventory = _weaponInventory;
                tacticalHud.PlayerHealth = GetComponent<NetworkHealth>();
            }
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleDeathCheck();
            if (SpectatorManager.Instance != null && SpectatorManager.Instance.isSpectating) return;

            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            {
                _currentMoveInput = Vector2.zero;
                _currentLookRotation = Vector2.zero;
                _isFirePressed = false;
                _isWalkingSlow = false;
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                bool isUiOpen = (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
                                (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen);

                if (!isUiOpen && Cursor.lockState != CursorLockMode.Locked)
                {
                    SetCursorLocked(true);
                }
            }

            UpdateInputStates();
            HandleCameraRotation();
            HandleMovementExecution();
            HandleWeaponCombatInput();
            UpdateSoldierAnimator();
        }

        HandleCrouchHeightTransition();
    }

    private void LateUpdate()
    {
        if (IsOwner && _spineBone != null && _characterController != null && _characterController.enabled)
        {
            _spineBone.Rotate(Vector3.right, _cameraPitch * 0.7f, Space.Self);
        }
    }

    private void UpdateSoldierAnimator()
    {
        if (_activeAnimator == null || !_activeAnimator.gameObject.activeInHierarchy)
        {
            _activeAnimator = GetComponentInChildren<Animator>(false);
        }

        if (_activeAnimator == null) return;

        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        _activeAnimator.SetFloat("Speed", currentSpeed);
        _activeAnimator.SetBool("IsCrouched", isCrouchedNet.Value);
        _activeAnimator.SetBool("IsGrounded", _characterController.isGrounded);
    }

    public void TriggerShootTPS()
    {
        if (_activeAnimator == null || !_activeAnimator.gameObject.activeInHierarchy)
        {
            _activeAnimator = GetComponentInChildren<Animator>(false);
        }

        if (_activeAnimator != null)
        {
            _activeAnimator.SetTrigger("Shoot");
        }
    }

    public void TriggerReloadTPS()
    {
        if (_activeAnimator == null || !_activeAnimator.gameObject.activeInHierarchy)
        {
            _activeAnimator = GetComponentInChildren<Animator>(false);
        }

        if (_activeAnimator != null)
        {
            _activeAnimator.ResetTrigger("Shoot");
            _activeAnimator.SetTrigger("Reload");
        }
    }

    private void HandleWeaponCombatInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase != RoundPhase.InProgress) return;

        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        if (bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing)) return;
        if (_weaponInventory == null) return;

        WeaponBase currentWeapon = _weaponInventory.ActiveWeapon;
        if (currentWeapon == null) return;

        bool reloadTriggered = false;
        if (InputManager.Instance != null) reloadTriggered = InputManager.Instance.WasReloadTriggered;
        if (!reloadTriggered && Keyboard.current != null) reloadTriggered = Keyboard.current.rKey.wasPressedThisFrame;

        if (reloadTriggered)
        {
            currentWeapon.Reload();
        }

        bool wantsToFire = _isFirePressed;

        if (InputManager.Instance != null)
        {
            wantsToFire = wantsToFire || InputManager.Instance.IsFirePressed || InputManager.Instance.WasFireTriggered;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            wantsToFire = true;
        }

        if (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed)
        {
            wantsToFire = true;
        }

        if (!wantsToFire) return;

        WeaponData weaponData = currentWeapon.WeaponData;
        bool isAutomatic = weaponData != null && weaponData.isAutomatic;

        if (currentWeapon.CanFire())
        {
            currentWeapon.Fire();
            if (!isAutomatic) _isFirePressed = false;
        }
    }

    public void OnFire(InputValue value) => _isFirePressed = value.isPressed;
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
            _currentMoveInput = Vector2.zero;
            _currentLookRotation = Vector2.zero;
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
            _isWalkingSlow = false;

            if (_characterController != null) _characterController.enabled = false;
            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(false);

            if (_weaponInventory != null && _weaponInventory.ActiveWeapon != null)
            {
                _weaponInventory.ActiveWeapon.gameObject.SetActive(false);
            }

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
        _verticalVelocity = 0f;
        _currentMoveInput = Vector2.zero;
        _currentLookRotation = Vector2.zero;
        _eventMoveInput = Vector2.zero;
        _hasInitiatedSpectate = false;
        _isWalkingSlow = false;

        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);

        if (IsOwner)
        {
            if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
            if (_playerCamera != null) _playerCamera.gameObject.SetActive(true);
            if (_characterController != null) _characterController.enabled = true;
        }
    }

    private void UpdateInputStates()
    {
        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        bool isInteractingBomb = bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing);
        bool isMovementFrozen = isInteractingBomb || (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase == RoundPhase.Warmup);

        if (isMovementFrozen)
        {
            _currentMoveInput = Vector2.zero;
            _isWalkingSlow = false;
            _jumpRequested = false;
            return;
        }

        Vector2 move = Vector2.zero;
        if (InputManager.Instance != null && InputManager.Instance.MovementInput.sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR)
        {
            move = InputManager.Instance.MovementInput;
        }
        else if (_eventMoveInput.sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR)
        {
            move = _eventMoveInput;
        }
        else
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
            }
            if (Gamepad.current != null && move.sqrMagnitude < MIN_MOVE_MAGNITUDE_SQR)
            {
                move = Gamepad.current.leftStick.ReadValue();
            }
        }
        _currentMoveInput = move;

        float gamepadSens = InputManager.Instance != null ? InputManager.Instance.GamepadSensitivity : PlayerPrefs.GetFloat("GamepadSensitivity", 75.0f);
        float mouseSens = InputManager.Instance != null ? InputManager.Instance.MouseSensitivity : PlayerPrefs.GetFloat("MouseSensitivity", _mouseSensitivity);
        Vector2 lookRotationDelta = Vector2.zero;
        bool isStickDeflected = Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR;

        if (isStickDeflected)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            if (stick.magnitude > 0.08f)
            {
                lookRotationDelta = stick * (gamepadSens * Time.deltaTime);
            }
        }
        else if (InputManager.Instance != null && InputManager.Instance.LookInput.sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR)
        {
            lookRotationDelta = InputManager.Instance.LookInput;
        }
        else if (_eventLookInput.sqrMagnitude > MIN_MOVE_MAGNITUDE_SQR)
        {
            lookRotationDelta = _eventLookInput * (mouseSens * 0.12f);
        }
        else if (Mouse.current != null)
        {
            lookRotationDelta = Mouse.current.delta.ReadValue() * (mouseSens * 0.12f);
        }

        if (_isStunned)
        {
            lookRotationDelta *= _stunSensRatio;
        }

        _currentLookRotation = lookRotationDelta;

        bool crouchPressed = false;
        bool walkSlowPressed = false;

        if (InputManager.Instance != null)
        {
            crouchPressed = InputManager.Instance.IsCrouchPressed;
            walkSlowPressed = InputManager.Instance.IsWalkPressed;
            if (InputManager.Instance.WasJumpTriggered && IsGrounded && !_isCrouching) _jumpRequested = true;
        }
        else
        {
            if (Keyboard.current != null)
            {
                crouchPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
                walkSlowPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded && !_isCrouching) _jumpRequested = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.isPressed || Gamepad.current.rightStickButton.isPressed) crouchPressed = true;
                if (Gamepad.current.leftStickButton.isPressed) walkSlowPressed = true;
                if (Gamepad.current.buttonSouth.wasPressedThisFrame && IsGrounded && !_isCrouching) _jumpRequested = true;
            }
        }

        _isWalkingSlow = walkSlowPressed;

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

    public void OnMove(InputValue value) => _eventMoveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _eventLookInput = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
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
        if (_currentLookRotation.sqrMagnitude < MIN_MOVE_MAGNITUDE_SQR) return;

        transform.Rotate(Vector3.up * _currentLookRotation.x);

        _cameraPitch -= _currentLookRotation.y;
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

        // Mantenemos la escala del modelo en 1 sin deformarlo: la animación se encarga de la postura
        if (_thirdPersonRoot != null && _thirdPersonRoot.transform.localScale != Vector3.one)
        {
            _thirdPersonRoot.transform.localScale = Vector3.one;
        }

        if (_hitboxesRoot != null && _hitboxesRoot.localScale != Vector3.one)
        {
            _hitboxesRoot.localScale = Vector3.one;
        }
    }

    private void HandleMovementExecution()
    {
        if (_characterController == null) return;
        if (!_characterController.enabled) _characterController.enabled = true;

        float baseSpeed = _activeRole != null ? _activeRole.walkSpeed : DEFAULT_WALK_SPEED;
        float currentSpeed = baseSpeed;

        if (_isCrouching) currentSpeed *= CROUCH_SPEED_RATIO;
        else if (_isWalkingSlow) currentSpeed *= WALK_SLOW_RATIO;
        if (_isStunned) currentSpeed *= _stunSlowRatio;

        Vector3 moveDirection = transform.right * _currentMoveInput.x + transform.forward * _currentMoveInput.y;
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
        _currentMoveInput = Vector2.zero;
        _currentLookRotation = Vector2.zero;
        _eventMoveInput = Vector2.zero;
        _hasInitiatedSpectate = false;
        _isWalkingSlow = false;

        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);

        if (IsOwner)
        {
            if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
            if (_playerCamera != null) _playerCamera.gameObject.SetActive(true);
            if (_characterController != null) _characterController.enabled = true;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ApplyStunClientRpc(float duration, float slowMultiplier, float sensMultiplier)
    {
        if (!IsOwner) return;

        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
        }

        _stunCoroutine = StartCoroutine(StunRoutine(duration, slowMultiplier, sensMultiplier));

        if (StunVisualEffect.Instance != null)
        {
            StunVisualEffect.Instance.TriggerStun(duration);
        }
    }

    private IEnumerator StunRoutine(float duration, float slowMultiplier, float sensMultiplier)
    {
        _isStunned = true;
        _stunSlowRatio = slowMultiplier;
        _stunSensRatio = sensMultiplier;

        yield return new WaitForSeconds(duration);

        _isStunned = false;
        _stunSlowRatio = 1.0f;
        _stunSensRatio = 1.0f;
        _stunCoroutine = null;
    }
}