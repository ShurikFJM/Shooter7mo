using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
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
    private const float CROUCH_CAMERA_HEIGHT_RATIO = 0.64f;
    private const float CROUCH_CAMERA_FORWARD_OFFSET = 0.14f;
    private const float MIN_MOVE_MAGNITUDE_SQR = 0.005f;
    private const float CAMERA_PLANE_DISTANCE = 1f;
    private const float LOCAL_CAMERA_NEAR_CLIP = 0.22f;
    private const float TORSO_HIDE_BACK_OFFSET = 0.22f;
    private const float TORSO_CROUCH_BACK_OFFSET = 0.34f;
    private const string LOBBY_CAMERA_TAG = "LobbyCamera";
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    private static readonly int ANIM_SPEED_HASH = Animator.StringToHash("Speed");
    private static readonly int ANIM_IS_CROUCHED_HASH = Animator.StringToHash("IsCrouched");
    private static readonly int ANIM_IS_GROUNDED_HASH = Animator.StringToHash("IsGrounded");
    private static readonly int ANIM_WEAPON_SLOT_HASH = Animator.StringToHash("WeaponSlot");
    private static readonly int ANIM_SHOOT_HASH = Animator.StringToHash("Shoot");
    private static readonly int ANIM_RELOAD_HASH = Animator.StringToHash("Reload");
    private static readonly int ANIM_THROW_GRENADE_HASH = Animator.StringToHash("ThrowGrenade");

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

    [SerializeField] private GameObject _infantryModel;
    [SerializeField] private GameObject _gunnerModel;
    [SerializeField] private GameObject _medicModel;

    [SerializeField] private PlayerRoleType _defaultTestingRole = PlayerRoleType.Tank;

    [SerializeField] private Transform _hitboxesRoot;
    [SerializeField] private LayerMask _obstacleLayers;

    private Animator _activeAnimator;
    private readonly HashSet<int> _cachedAnimatorParams = new HashSet<int>();

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

    public NetworkVariable<float> netMoveSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<bool> netIsGrounded = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<float> netCameraPitch = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
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
    private Transform _chestBone;
    private Transform _neckBone;
    private Transform _headBone;
    private Transform _leftUpperArmBone;
    private Transform _rightUpperArmBone;

    private float _verticalVelocity;
    private Vector2 _currentMoveInput;
    private Vector2 _currentLookRotation;
    private bool _isWalkingSlow;
    private bool _isCrouching;
    private bool _jumpRequested;
    private bool _isFirePressed;
    private float _cameraPitch;
    private float _defaultCameraLocalY;
    private float _defaultCameraLocalZ;
    private bool _hasInitiatedSpectate;
    private PlayerRoleType _lastAppliedRole = (PlayerRoleType)(-1);

    private void Awake()
    {
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_playerCamera == null) _playerCamera = GetComponentInChildren<Camera>(true);

        if (_cameraRoot == null)
        {
            Transform foundCamRoot = transform.Find("CameraRoot");
            if (foundCamRoot != null)
            {
                _cameraRoot = foundCamRoot;
            }
            else if (_playerCamera != null && _playerCamera.transform.parent != null && _playerCamera.transform.parent != transform)
            {
                _cameraRoot = _playerCamera.transform.parent;
            }
            else if (_playerCamera != null)
            {
                _cameraRoot = _playerCamera.transform;
            }
            else
            {
                _cameraRoot = transform;
            }
        }

        if (_cameraRoot != null)
        {
            _cameraRoot.gameObject.SetActive(true);
            _defaultCameraLocalY = _cameraRoot.localPosition.y;
            _defaultCameraLocalZ = _cameraRoot.localPosition.z;
        }

        if (_audioListener == null && _playerCamera != null) _audioListener = _playerCamera.GetComponent<AudioListener>();

        _networkHealth = GetComponent<NetworkHealth>();
        _playerTeam = GetComponent<PlayerTeam>();
        _weaponInventory = GetComponentInChildren<WeaponInventory>(true);
        _playerMovementAudio = GetComponent<PlayerMovementAudio>();

        SafeDisableFirstPersonRoot();
        EnsureRoleDatabaseReference();
        EnsureModelReferences();
        LocateBodyBones();
    }

    private void SafeDisableFirstPersonRoot()
    {
        if (_firstPersonRoot == null) return;

        if (_firstPersonRoot == gameObject ||
            (_cameraRoot != null && (_firstPersonRoot == _cameraRoot.gameObject || _cameraRoot.IsChildOf(_firstPersonRoot.transform))) ||
            (_playerCamera != null && (_firstPersonRoot == _playerCamera.gameObject || _playerCamera.transform.IsChildOf(_firstPersonRoot.transform))) ||
            (_thirdPersonRoot != null && (_firstPersonRoot == _thirdPersonRoot || _thirdPersonRoot.transform.IsChildOf(_firstPersonRoot.transform))))
        {
            _firstPersonRoot = null;
            return;
        }

        _firstPersonRoot.SetActive(false);
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

    private Animator ResolveActiveAnimator()
    {
        if (_activeAnimator == null || !_activeAnimator.gameObject.activeInHierarchy)
        {
            _activeAnimator = GetComponentInChildren<Animator>(false);
            _cachedAnimatorParams.Clear();

            if (_activeAnimator != null)
            {
                _activeAnimator.applyRootMotion = false;
                AnimatorControllerParameter[] parameters = _activeAnimator.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    _cachedAnimatorParams.Add(parameters[i].nameHash);
                }
            }
        }

        return _activeAnimator;
    }

    public void LocateBodyBones()
    {
        _spineBone = null;
        _chestBone = null;
        _neckBone = null;
        _headBone = null;
        _leftUpperArmBone = null;
        _rightUpperArmBone = null;

        Animator anim = ResolveActiveAnimator();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        if (anim != null && anim.isHuman)
        {
            _spineBone = anim.GetBoneTransform(HumanBodyBones.Spine);
            _chestBone = anim.GetBoneTransform(HumanBodyBones.UpperChest);
            if (_chestBone == null) _chestBone = anim.GetBoneTransform(HumanBodyBones.Chest);
            _neckBone = anim.GetBoneTransform(HumanBodyBones.Neck);
            _headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            _leftUpperArmBone = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rightUpperArmBone = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        }

        Transform searchRoot = anim != null ? anim.transform : (_thirdPersonRoot != null ? _thirdPersonRoot.transform : transform);
        Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            string bName = transforms[i].name;
            if (_spineBone == null && bName.IndexOf("Spine", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _spineBone = transforms[i];
            if (_chestBone == null && (bName.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) >= 0 || bName.IndexOf("Spine1", System.StringComparison.OrdinalIgnoreCase) >= 0))
                _chestBone = transforms[i];
            if (_neckBone == null && bName.IndexOf("Neck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _neckBone = transforms[i];
            if (_headBone == null && bName.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _headBone = transforms[i];
            if (_leftUpperArmBone == null && (bName.IndexOf("LeftUpperArm", System.StringComparison.OrdinalIgnoreCase) >= 0 || bName.IndexOf("L UpperArm", System.StringComparison.OrdinalIgnoreCase) >= 0 || bName.IndexOf("LeftArm", System.StringComparison.OrdinalIgnoreCase) >= 0))
                _leftUpperArmBone = transforms[i];
            if (_rightUpperArmBone == null && (bName.IndexOf("RightUpperArm", System.StringComparison.OrdinalIgnoreCase) >= 0 || bName.IndexOf("R UpperArm", System.StringComparison.OrdinalIgnoreCase) >= 0 || bName.IndexOf("RightArm", System.StringComparison.OrdinalIgnoreCase) >= 0))
                _rightUpperArmBone = transforms[i];
        }
    }

    private bool IsLobbySpectator()
    {
        if (_playerTeam != null && _playerTeam.CurrentTeam.Value == Team.Neutral)
        {
            if (CustomLobbyManager.SavedTeams != null &&
                CustomLobbyManager.SavedTeams.TryGetValue(OwnerClientId, out Team savedTeam) &&
                savedTeam != Team.Neutral)
            {
                return false;
            }

            if (_networkHealth != null && !_networkHealth.IsAlive.Value && _networkHealth.CurrentHealth.Value <= 0f)
            {
                return true;
            }
        }

        if (CustomLobbyManager.SavedTeams != null &&
            CustomLobbyManager.SavedTeams.TryGetValue(OwnerClientId, out Team lobbyTeam) &&
            lobbyTeam == Team.Neutral)
        {
            return true;
        }

        return false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EnsureRoleDatabaseReference();
        EnsureModelReferences();
        LocateBodyBones();

        if (_cameraRoot != null) _cameraRoot.gameObject.SetActive(true);
        SafeDisableFirstPersonRoot();

        selectedRole.OnValueChanged += HandleRoleChanged;
        if (_weaponInventory == null) _weaponInventory = GetComponentInChildren<WeaponInventory>(true);
        if (_networkHealth != null) _networkHealth.CurrentHealth.OnValueChanged += HandleHealthChanged;
        if (_playerTeam != null) _playerTeam.CurrentTeam.OnValueChanged += HandleTeamValueChanged;

        Canvas localPlayerCanvas = GetComponentInChildren<Canvas>(true);

        if (IsOwner)
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.EnableAllGameplayActions();
            }

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
            }

            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(true);
                _playerCamera.enabled = true;
                _playerCamera.nearClipPlane = LOCAL_CAMERA_NEAR_CLIP;
            }
            if (_audioListener != null) _audioListener.enabled = true;
            if (localPlayerCanvas != null) localPlayerCanvas.gameObject.SetActive(false);

            Camera lobbyCamera = GameObject.FindWithTag(LOBBY_CAMERA_TAG)?.GetComponent<Camera>();
            if (lobbyCamera != null) lobbyCamera.gameObject.SetActive(false);

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && _playerCamera != null)
                {
                    canvas.worldCamera = _playerCamera;
                    canvas.planeDistance = CAMERA_PLANE_DISTANCE;
                }
            }

            if (CursorStateManager.Instance != null)
            {
                CursorStateManager.Instance.OnCursorLockStateChanged += HandleCursorLockStateChanged;
                CursorStateManager.Instance.ForceEvaluateState();
            }
            else
            {
                SetCursorLocked(true);
            }

            if (IsLobbySpectator())
            {
                EnterLobbySpectatorMode();
                return;
            }

            if (_characterController != null) _characterController.enabled = true;
            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);

            ConfigureLocalBodyVisibility();

            TacticalHUD tacticalHud = FindAnyObjectByType<TacticalHUD>();
            if (tacticalHud != null)
            {
                tacticalHud.PlayerHealth = GetComponent<NetworkHealth>();
                tacticalHud.Inventory = _weaponInventory;
            }

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

            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(true);
                _playerCamera.enabled = false;
            }
            if (_audioListener != null) _audioListener.enabled = false;

            bool isRemoteSpectator = _playerTeam != null && _playerTeam.CurrentTeam.Value == Team.Neutral &&
                                     _networkHealth != null && !_networkHealth.IsAlive.Value;

            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(!isRemoteSpectator);

            if (!isRemoteSpectator)
            {
                ApplyRoleData(selectedRole.Value);
            }
        }
    }

    private void EnterLobbySpectatorMode()
    {
        _hasInitiatedSpectate = true;
        if (_characterController != null) _characterController.enabled = false;
        if (_cameraRoot != null) _cameraRoot.gameObject.SetActive(true);
        if (_playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(true);
            _playerCamera.enabled = true;
        }
        SafeDisableFirstPersonRoot();
        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(false);

        if (SpectatorManager.Instance != null && _playerCamera != null)
        {
            SpectatorManager.Instance.StartSpectating(Team.Neutral, _playerCamera, transform, _defaultCameraLocalY);
        }
    }

    private void HandleTeamValueChanged(Team previousTeam, Team currentTeam)
    {
        if (IsOwner && currentTeam == Team.Neutral && _networkHealth != null && !_networkHealth.IsAlive.Value)
        {
            EnterLobbySpectatorMode();
        }
        else if (!IsOwner && _thirdPersonRoot != null)
        {
            bool isRemoteSpectator = currentTeam == Team.Neutral && _networkHealth != null && !_networkHealth.IsAlive.Value;
            _thirdPersonRoot.SetActive(!isRemoteSpectator);
        }
    }

    public void ConfigureLocalBodyVisibility()
    {
        if (!IsOwner) return;

        GameObject bodyRoot = _thirdPersonRoot != null ? _thirdPersonRoot : gameObject;
        Renderer[] renderers = bodyRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.enabled = true;
            r.shadowCastingMode = ShadowCastingMode.On;
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
        if (_cameraRoot != null) _cameraRoot.gameObject.SetActive(true);
        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(visible);
        SafeDisableFirstPersonRoot();

        if (visible && IsOwner)
        {
            ConfigureLocalBodyVisibility();
        }

        if (_weaponInventory != null && _weaponInventory.ActiveWeapon != null)
        {
            _weaponInventory.ActiveWeapon.gameObject.SetActive(visible);
        }
    }

    public override void OnNetworkDespawn()
    {
        selectedRole.OnValueChanged -= HandleRoleChanged;
        if (_networkHealth != null) _networkHealth.CurrentHealth.OnValueChanged -= HandleHealthChanged;
        if (_playerTeam != null) _playerTeam.CurrentTeam.OnValueChanged -= HandleTeamValueChanged;
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
        if (_playerTeam != null && _playerTeam.CurrentTeam.Value == Team.Neutral &&
            _networkHealth != null && !_networkHealth.IsAlive.Value)
        {
            if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(false);
            return;
        }

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
        _cachedAnimatorParams.Clear();
        LocateBodyBones();

        PlayerTeamVisualizer visualizer = GetComponent<PlayerTeamVisualizer>();
        if (visualizer != null)
        {
            visualizer.RefreshRenderers();
        }

        if (IsOwner)
        {
            ConfigureLocalBodyVisibility();
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

            if (SpectatorManager.Instance != null && SpectatorManager.Instance.isSpectating)
            {
                return;
            }

            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            {
                _currentMoveInput = Vector2.zero;
                _currentLookRotation = Vector2.zero;
                _isFirePressed = false;
                _isWalkingSlow = false;
                _jumpRequested = false;

                HandleMovementExecution();
                UpdateSoldierAnimator();
                HandleCrouchHeightTransition();
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
        }
        else if (_cameraRoot != null)
        {
            _cameraRoot.localRotation = Quaternion.Euler(netCameraPitch.Value, 0f, 0f);
        }

        UpdateSoldierAnimator();
        HandleCrouchHeightTransition();
    }

    private void LateUpdate()
    {
        if (!IsOwner || _hasInitiatedSpectate) return;

        if (_spineBone != null && _characterController != null && _characterController.enabled)
        {
            _spineBone.Rotate(Vector3.right, _cameraPitch * 0.65f, Space.Self);
        }

        if (_headBone != null) _headBone.localScale = Vector3.zero;
        if (_neckBone != null) _neckBone.localScale = Vector3.zero;

        Transform torsoToShift = _chestBone != null ? _chestBone : _spineBone;
        if (torsoToShift != null)
        {
            Vector3 savedLeftArmPos = _leftUpperArmBone != null ? _leftUpperArmBone.position : Vector3.zero;
            Vector3 savedRightArmPos = _rightUpperArmBone != null ? _rightUpperArmBone.position : Vector3.zero;

            float backOffset = _isCrouching ? TORSO_CROUCH_BACK_OFFSET : TORSO_HIDE_BACK_OFFSET;
            torsoToShift.position -= transform.forward * backOffset;

            if (_leftUpperArmBone != null) _leftUpperArmBone.position = savedLeftArmPos;
            if (_rightUpperArmBone != null) _rightUpperArmBone.position = savedRightArmPos;
        }
    }

    private void UpdateSoldierAnimator()
    {
        Animator anim = ResolveActiveAnimator();
        if (anim == null || anim.runtimeAnimatorController == null) return;

        float currentSpeed = 0f;
        bool isGrounded = true;

        if (IsOwner)
        {
            if (_characterController != null && _characterController.enabled)
            {
                Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
                currentSpeed = horizontalVelocity.magnitude;
                isGrounded = _characterController.isGrounded;
            }

            if (IsSpawned)
            {
                if (Mathf.Abs(netMoveSpeed.Value - currentSpeed) > 0.05f)
                {
                    netMoveSpeed.Value = currentSpeed;
                }
                if (netIsGrounded.Value != isGrounded)
                {
                    netIsGrounded.Value = isGrounded;
                }
            }
        }
        else
        {
            currentSpeed = netMoveSpeed.Value;
            isGrounded = netIsGrounded.Value;
        }

        bool crouchedState = IsOwner ? _isCrouching : isCrouchedNet.Value;
        int activeSlot = _weaponInventory != null ? _weaponInventory.ActiveSlotIndex : 1;

        if (_cachedAnimatorParams.Contains(ANIM_SPEED_HASH))
            anim.SetFloat(ANIM_SPEED_HASH, currentSpeed);

        if (_cachedAnimatorParams.Contains(ANIM_IS_CROUCHED_HASH))
            anim.SetBool(ANIM_IS_CROUCHED_HASH, crouchedState);

        if (_cachedAnimatorParams.Contains(ANIM_IS_GROUNDED_HASH))
            anim.SetBool(ANIM_IS_GROUNDED_HASH, isGrounded);

        if (_cachedAnimatorParams.Contains(ANIM_WEAPON_SLOT_HASH))
            anim.SetInteger(ANIM_WEAPON_SLOT_HASH, activeSlot);
    }

    public void TriggerShootTPS()
    {
        PlaySoldierActionLocal(ANIM_SHOOT_HASH);
        if (IsOwner && IsSpawned)
        {
            PlaySoldierActionServerRpc(0);
        }
    }

    public void TriggerReloadTPS()
    {
        PlaySoldierActionLocal(ANIM_RELOAD_HASH);
        if (IsOwner && IsSpawned)
        {
            PlaySoldierActionServerRpc(1);
        }
    }

    public void TriggerGrenadeTPS()
    {
        PlaySoldierActionLocal(ANIM_THROW_GRENADE_HASH);
        if (IsOwner && IsSpawned)
        {
            PlaySoldierActionServerRpc(2);
        }
    }

    private void PlaySoldierActionLocal(int triggerHash)
    {
        Animator anim = ResolveActiveAnimator();
        if (anim == null || anim.runtimeAnimatorController == null) return;

        if (triggerHash != ANIM_SHOOT_HASH && _cachedAnimatorParams.Contains(ANIM_SHOOT_HASH))
        {
            anim.ResetTrigger(ANIM_SHOOT_HASH);
        }

        if (_cachedAnimatorParams.Contains(triggerHash))
        {
            anim.SetTrigger(triggerHash);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlaySoldierActionServerRpc(byte actionType)
    {
        PlaySoldierActionClientRpc(actionType);
    }

    [Rpc(SendTo.NotOwner)]
    private void PlaySoldierActionClientRpc(byte actionType)
    {
        if (actionType == 0) PlaySoldierActionLocal(ANIM_SHOOT_HASH);
        else if (actionType == 1) PlaySoldierActionLocal(ANIM_RELOAD_HASH);
        else if (actionType == 2) PlaySoldierActionLocal(ANIM_THROW_GRENADE_HASH);
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
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
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
            _cameraRoot.gameObject.SetActive(true);
            _cameraRoot.localPosition = new Vector3(0f, _defaultCameraLocalY, _defaultCameraLocalZ);
            _cameraRoot.localRotation = Quaternion.identity;
        }

        _cameraPitch = 0f;
        _verticalVelocity = 0f;
        _currentMoveInput = Vector2.zero;
        _currentLookRotation = Vector2.zero;
        _hasInitiatedSpectate = false;
        _isWalkingSlow = false;

        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);

        if (IsOwner)
        {
            ConfigureLocalBodyVisibility();
            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(true);
                _playerCamera.enabled = true;
            }
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

        bool isStickDeflected = Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.01f;

        if (isStickDeflected)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            lookRotationDelta = stick * (gamepadSens * Time.deltaTime);
        }
        else if (Mouse.current != null)
        {
            lookRotationDelta = Mouse.current.delta.ReadValue() * (mouseSens * 0.12f);
        }
        else if (InputManager.Instance != null)
        {
            lookRotationDelta = InputManager.Instance.LookInput * 0.12f;
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

    public void OnMove(InputValue value)
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        _currentMoveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value) { }

    public void OnJump(InputValue value)
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
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
        if (_currentLookRotation.sqrMagnitude <= 0.000001f) return;

        transform.Rotate(Vector3.up * _currentLookRotation.x);

        _cameraPitch -= _currentLookRotation.y;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -_upDownLookLimit, _upDownLookLimit);
        _cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);

        if (IsSpawned && Mathf.Abs(netCameraPitch.Value - _cameraPitch) > 0.1f)
        {
            netCameraPitch.Value = _cameraPitch;
        }
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
            float targetCameraY = crouched ? _defaultCameraLocalY * CROUCH_CAMERA_HEIGHT_RATIO : _defaultCameraLocalY;
            float targetCameraZ = crouched ? _defaultCameraLocalZ + CROUCH_CAMERA_FORWARD_OFFSET : _defaultCameraLocalZ;

            Vector3 targetCameraPosition = new Vector3(_cameraRoot.localPosition.x, targetCameraY, targetCameraZ);
            _cameraRoot.localPosition = Vector3.Lerp(_cameraRoot.localPosition, targetCameraPosition, Time.deltaTime * CROUCH_TRANSITION_SPEED);
        }

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
            _cameraRoot.gameObject.SetActive(true);
            _cameraRoot.localPosition = new Vector3(0f, _defaultCameraLocalY, _defaultCameraLocalZ);
            _cameraRoot.localRotation = Quaternion.identity;
        }

        _cameraPitch = 0f;
        _verticalVelocity = 0f;
        _currentMoveInput = Vector2.zero;
        _currentLookRotation = Vector2.zero;
        _hasInitiatedSpectate = false;
        _isWalkingSlow = false;

        if (_thirdPersonRoot != null) _thirdPersonRoot.SetActive(true);

        if (IsOwner)
        {
            ConfigureLocalBodyVisibility();
            if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(true);
                _playerCamera.enabled = true;
            }
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