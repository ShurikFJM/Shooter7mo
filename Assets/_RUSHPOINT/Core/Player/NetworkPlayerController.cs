using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    private const float _GRAVITY = -19.62f;
    private const float _DEFAULT_WALK_SPEED = 5f;
    private const float _DEFAULT_SPRINT_SPEED = 8f;
    private const float _DEFAULT_JUMP_FORCE = 1.5f;
    private const float _CROUCH_HEIGHT = 1f;
    private const float _STANDING_HEIGHT = 2f;
    private const float _CROUCH_SPEED_RATIO = 0.5f;
    private const float _WALK_SLOW_RATIO = 0.5f;
    private const float _CROUCH_TRANSITION_SPEED = 12f;
    private const float _MIN_MOVE_MAGNITUDE_SQR = 0.01f;
    private const float _CAMERA_PLANE_DISTANCE = 1f;
    private const string _LOBBY_CAMERA_TAG = "LobbyCamera";

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

    public NetworkVariable<PlayerRoleType> SelectedRole = new NetworkVariable<PlayerRoleType>(
        PlayerRoleType.Assault,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsCrouchedNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private RoleDataSO _activeRole;
    private Vector3 _verticalVelocity;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isSprinting;
    private bool _isWalkingSlow;
    private bool _isCrouching;
    private bool _jumpRequested;
    private float _cameraPitch;
    private float _defaultCameraLocalY;

    public Camera PlayerCamera => _playerCamera;
    public bool IsGrounded => _characterController != null && _characterController.isGrounded;
    public bool IsMoving => _moveInput.sqrMagnitude > _MIN_MOVE_MAGNITUDE_SQR;
    public RoleDataSO ActiveRole => _activeRole;

    private void Awake()
    {
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }

        if (_playerCamera == null)
        {
            _playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (_cameraRoot == null)
        {
            _cameraRoot = _playerCamera != null ? _playerCamera.transform : transform;
        }

        if (_cameraRoot != null)
        {
            _defaultCameraLocalY = _cameraRoot.localPosition.y;
        }

        if (_audioListener == null && _playerCamera != null)
        {
            _audioListener = _playerCamera.GetComponent<AudioListener>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SelectedRole.OnValueChanged += HandleRoleChanged;
        ApplyRoleData(SelectedRole.Value);

        if (IsOwner)
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
            }

            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(true);
            }

            if (_audioListener != null)
            {
                _audioListener.enabled = true;
            }

            if (_firstPersonRoot != null)
            {
                _firstPersonRoot.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && _playerCamera != null)
                {
                    canvas.worldCamera = _playerCamera;
                    canvas.planeDistance = _CAMERA_PLANE_DISTANCE;
                }
            }

            TacticalHUD tacticalHud = FindAnyObjectByType<TacticalHUD>();
            if (tacticalHud != null)
            {
                tacticalHud.PlayerHealth = GetComponent<NetworkHealth>();
                tacticalHud.Inventory = GetComponentInChildren<WeaponInventory>();
            }

            Camera lobbyCamera = GameObject.FindWithTag(_LOBBY_CAMERA_TAG)?.GetComponent<Camera>();
            if (lobbyCamera != null)
            {
                lobbyCamera.gameObject.SetActive(false);
            }
        }
        else
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(false);
            }

            if (_audioListener != null)
            {
                _audioListener.enabled = false;
            }

            if (_firstPersonRoot != null)
            {
                _firstPersonRoot.SetActive(false);
            }
        }
    }

    public void SetInitialRole(PlayerRoleType roleType)
    {
        if (IsServer)
        {
            SelectedRole.Value = roleType;
        }
        ApplyRoleData(roleType);
    }

    public override void OnNetworkDespawn()
    {
        SelectedRole.OnValueChanged -= HandleRoleChanged;
    }

    private void HandleRoleChanged(PlayerRoleType previousRole, PlayerRoleType currentRole)
    {
        ApplyRoleData(currentRole);
    }

    private void ApplyRoleData(PlayerRoleType roleType)
    {
        if (_roleDatabase == null) return;

        _activeRole = _roleDatabase.GetRole(roleType);

        if (IsServer && _activeRole != null)
        {
            NetworkHealth networkHealth = GetComponent<NetworkHealth>();
            if (networkHealth != null)
            {
                networkHealth.SetMaxStatsServer(_activeRole.maxHealth, _activeRole.maxArmor);
            }
        }
    }

    private void Update()
    {
        if (_activeRole == null)
        {
            ApplyRoleData(SelectedRole.Value);
        }

        if (IsOwner)
        {
            UpdateInputStates();
            HandleCameraRotation();
            HandleMovementExecution();
        }

        HandleCrouchHeightTransition();
    }

    private void UpdateInputStates()
    {
        bool isFreezeTime = RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value == RoundPhase.FreezeTime;
        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        bool isInteractingBomb = bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing);

        if (isFreezeTime || isInteractingBomb)
        {
            _isSprinting = false;
            _isWalkingSlow = false;
            return;
        }

        bool crouchPressed = false;
        bool walkSlowPressed = false;
        bool sprintPressed = false;

        if (Keyboard.current != null)
        {
            crouchPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
            walkSlowPressed = Keyboard.current.leftShiftKey.isPressed;
            sprintPressed = Keyboard.current.spaceKey.isPressed && Keyboard.current.wKey.isPressed;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonEast.isPressed || Gamepad.current.rightStickButton.isPressed)
            {
                crouchPressed = true;
            }
            if (Gamepad.current.leftStickButton.isPressed)
            {
                sprintPressed = true;
            }
        }

        _isWalkingSlow = walkSlowPressed;
        _isSprinting = sprintPressed;

        bool targetCrouch = _isCrouching;
        if (crouchPressed)
        {
            targetCrouch = true;
        }
        else
        {
            if (!HasCeilingObstacle())
            {
                targetCrouch = false;
            }
        }

        if (targetCrouch != _isCrouching)
        {
            _isCrouching = targetCrouch;
            SetCrouchStateServerRpc(_isCrouching);
        }
    }

    [ServerRpc]
    private void SetCrouchStateServerRpc(bool crouching)
    {
        IsCrouchedNet.Value = crouching;
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        bool isFreezeTime = RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value == RoundPhase.FreezeTime;
        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        bool isInteractingBomb = bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing);

        if (isFreezeTime || isInteractingBomb) return;

        if (value.isPressed && IsGrounded && !_isCrouching)
        {
            _jumpRequested = true;
        }
    }

    private bool HasCeilingObstacle()
    {
        if (_obstacleLayers.value == 0 || _characterController == null) return false;

        float radius = _characterController.radius * 0.7f;
        Vector3 pointBottom = transform.position + Vector3.up * (_CROUCH_HEIGHT + 0.1f);
        Vector3 pointTop = transform.position + Vector3.up * (_STANDING_HEIGHT - radius);

        if (pointTop.y <= pointBottom.y) return false;

        Collider[] hits = Physics.OverlapCapsule(pointBottom, pointTop, radius, _obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject != gameObject && !hits[i].transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private void HandleCameraRotation()
    {
        if (_cameraRoot == null) return;

        float mouseYaw = _lookInput.x * _mouseSensitivity;
        transform.Rotate(Vector3.up * mouseYaw);

        _cameraPitch -= _lookInput.y * _mouseSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -_upDownLookLimit, _upDownLookLimit);
        _cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    private void HandleCrouchHeightTransition()
    {
        bool crouched = IsOwner ? _isCrouching : IsCrouchedNet.Value;

        if (_characterController != null && IsOwner)
        {
            float targetHeight = crouched ? _CROUCH_HEIGHT : _STANDING_HEIGHT;
            _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * _CROUCH_TRANSITION_SPEED);
            _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);
        }

        if (_cameraRoot != null && IsOwner)
        {
            float targetCameraY = crouched ? _defaultCameraLocalY * 0.55f : _defaultCameraLocalY;
            Vector3 targetCameraPosition = _cameraRoot.localPosition;
            targetCameraPosition.y = Mathf.Lerp(targetCameraPosition.y, targetCameraY, Time.deltaTime * _CROUCH_TRANSITION_SPEED);
            _cameraRoot.localPosition = targetCameraPosition;
        }

        float targetScaleY = crouched ? 0.5f : 1f;

        if (_thirdPersonRoot != null)
        {
            Vector3 visualScale = _thirdPersonRoot.transform.localScale;
            visualScale.y = Mathf.Lerp(visualScale.y, targetScaleY, Time.deltaTime * _CROUCH_TRANSITION_SPEED);
            _thirdPersonRoot.transform.localScale = visualScale;
        }

        if (_hitboxesRoot != null)
        {
            Vector3 hitboxScale = _hitboxesRoot.localScale;
            hitboxScale.y = Mathf.Lerp(hitboxScale.y, targetScaleY, Time.deltaTime * _CROUCH_TRANSITION_SPEED);
            _hitboxesRoot.localScale = hitboxScale;
        }
    }

    private void HandleMovementExecution()
    {
        if (_characterController == null) return;

        bool isFreezeTime = RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value == RoundPhase.FreezeTime;
        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        bool isInteractingBomb = bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing);

        Vector2 effectiveInput = (isFreezeTime || isInteractingBomb) ? Vector2.zero : _moveInput;

        if (IsGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = -2f;
        }

        float baseWalkSpeed = _activeRole != null && _activeRole.walkSpeed > 0f ? _activeRole.walkSpeed : _DEFAULT_WALK_SPEED;
        float baseSprintSpeed = _activeRole != null && _activeRole.sprintSpeed > 0f ? _activeRole.sprintSpeed : _DEFAULT_SPRINT_SPEED;
        float baseJumpForce = _activeRole != null && _activeRole.jumpForce > 0f ? _activeRole.jumpForce : _DEFAULT_JUMP_FORCE;

        float movementSpeed = baseWalkSpeed;

        if (_isCrouching)
        {
            movementSpeed = baseWalkSpeed * _CROUCH_SPEED_RATIO;
        }
        else if (_isWalkingSlow)
        {
            movementSpeed = baseWalkSpeed * _WALK_SLOW_RATIO;
        }
        else if (_isSprinting)
        {
            movementSpeed = baseSprintSpeed;
        }

        Vector3 moveDirection = transform.right * effectiveInput.x + transform.forward * effectiveInput.y;
        _characterController.Move(moveDirection * movementSpeed * Time.deltaTime);

        if (_jumpRequested && IsGrounded)
        {
            if (!isFreezeTime && !isInteractingBomb)
            {
                _verticalVelocity.y = Mathf.Sqrt(baseJumpForce * -2f * _GRAVITY);
            }
            _jumpRequested = false;
        }

        _verticalVelocity.y += _GRAVITY * Time.deltaTime;
        _characterController.Move(_verticalVelocity * Time.deltaTime);
    }
}