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
    private const float _CROUCH_TRANSITION_SPEED = 10f;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _upDownLookLimit = 85f;
    [SerializeField] private RoleDatabaseSO _roleDatabase;
    [SerializeField] private GameObject _firstPersonRoot;
    [SerializeField] private GameObject _thirdPersonRoot;

    public NetworkVariable<PlayerRoleType> SelectedRole = new NetworkVariable<PlayerRoleType>(
        PlayerRoleType.Assault,
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
    public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
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
                playerInput.enabled = false;
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

            if (_thirdPersonRoot != null)
            {
                _thirdPersonRoot.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && _playerCamera != null)
                {
                    canvas.worldCamera = _playerCamera;
                    canvas.planeDistance = 1f;
                }
            }

            TacticalHUD tacticalHud = FindAnyObjectByType<TacticalHUD>();
            if (tacticalHud != null)
            {
                tacticalHud.playerHealth = GetComponent<NetworkHealth>();
                tacticalHud.inventory = GetComponentInChildren<WeaponInventory>();
            }

            Camera lobbyCamera = GameObject.FindWithTag("LobbyCamera")?.GetComponent<Camera>();
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

            if (_thirdPersonRoot != null)
            {
                _thirdPersonRoot.SetActive(true);
            }
        }
    }

    public void SetInitialRole(PlayerRoleType roleType)
    {
        SelectedRole.Value = roleType;
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
        if (!IsOwner) return;

        if (_activeRole == null)
        {
            ApplyRoleData(SelectedRole.Value);
        }

        HandleCameraRotation();
        HandleCrouchHeightTransition();
        HandleMovementExecution();
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    public void OnSprint(InputValue value) => _isSprinting = value.isPressed;
    public void OnWalk(InputValue value) => _isWalkingSlow = value.isPressed;
    public void OnCrouch(InputValue value) => _isCrouching = value.isPressed;

    public void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded && !_isCrouching)
        {
            _jumpRequested = true;
        }
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
        if (_characterController == null || _cameraRoot == null) return;

        float targetHeight = _isCrouching ? _CROUCH_HEIGHT : _STANDING_HEIGHT;
        _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * _CROUCH_TRANSITION_SPEED);
        _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);

        float targetCameraY = _isCrouching ? _defaultCameraLocalY * 0.5f : _defaultCameraLocalY;
        Vector3 targetCameraPosition = _cameraRoot.localPosition;
        targetCameraPosition.y = Mathf.Lerp(targetCameraPosition.y, targetCameraY, Time.deltaTime * _CROUCH_TRANSITION_SPEED);
        _cameraRoot.localPosition = targetCameraPosition;
    }

    private void HandleMovementExecution()
    {
        if (_characterController == null) return;

        BombInteractor bombInteractor = GetComponent<BombInteractor>();
        if (bombInteractor != null && bombInteractor.IsPlanting)
        {
            _moveInput = Vector2.zero;
        }

        if (IsGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = -2f;
        }

        float baseWalkSpeed = _activeRole != null ? _activeRole.walkSpeed : _DEFAULT_WALK_SPEED;
        float baseSprintSpeed = _activeRole != null ? _activeRole.sprintSpeed : _DEFAULT_SPRINT_SPEED;
        float baseJumpForce = _activeRole != null ? _activeRole.jumpForce : _DEFAULT_JUMP_FORCE;

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

        Vector3 moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _characterController.Move(moveDirection * movementSpeed * Time.deltaTime);

        if (_jumpRequested && IsGrounded)
        {
            if (bombInteractor == null || !bombInteractor.IsPlanting)
            {
                _verticalVelocity.y = Mathf.Sqrt(baseJumpForce * -2f * _GRAVITY);
            }
            _jumpRequested = false;
        }

        _verticalVelocity.y += _GRAVITY * Time.deltaTime;
        _characterController.Move(_verticalVelocity * Time.deltaTime);
    }
}