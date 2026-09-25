using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float upDownLookLimit = 85f;

    [Header("Role Configuration")]
    [SerializeField] private RoleDatabaseSO roleDatabase;
    private RoleDataSO activeRole;

    [Header("Visual Meshes")]
    [SerializeField] private GameObject firstPersonRoot;
    [SerializeField] private GameObject thirdPersonRoot;

    public NetworkVariable<PlayerRoleType> SelectedRole = new NetworkVariable<PlayerRoleType>(
        PlayerRoleType.Assault,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float gravity = -19.62f;
    private Vector3 verticalVelocity;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool jumpRequested;
    private float cameraPitch = 0f;

    public Camera PlayerCamera => playerCamera;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public RoleDataSO ActiveRole => activeRole;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Escuchar cambios de rol y cargar las estadísticas iniciales
        SelectedRole.OnValueChanged += OnRoleChanged;
        ApplyRoleData(SelectedRole.Value);

        if (IsOwner)
        {
            // Restablecer foco del Input System
            PlayerInput pInput = GetComponent<PlayerInput>();
            if (pInput != null)
            {
                pInput.enabled = false;
                pInput.enabled = true;
                pInput.ActivateInput();
            }

            playerCamera.gameObject.SetActive(true);
            audioListener.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (firstPersonRoot != null) firstPersonRoot.SetActive(true);
            if (thirdPersonRoot != null) thirdPersonRoot.SetActive(false);

            TacticalHUD hud = FindAnyObjectByType<TacticalHUD>();
            if (hud != null)
            {
                hud.playerHealth = GetComponent<NetworkHealth>();
                hud.inventory = GetComponentInChildren<WeaponInventory>();
            }
        }
        else
        {
            playerCamera.gameObject.SetActive(false);
            audioListener.enabled = false;

            if (firstPersonRoot != null) firstPersonRoot.SetActive(false);
            if (thirdPersonRoot != null) thirdPersonRoot.SetActive(true);
        }
    }

    public void SetInitialRole(PlayerRoleType role)
    {
        SelectedRole.Value = role;
        ApplyRoleData(role);
    }

    public override void OnNetworkDespawn()
    {
        SelectedRole.OnValueChanged -= OnRoleChanged;
    }

    private void OnRoleChanged(PlayerRoleType previous, PlayerRoleType current)
    {
        ApplyRoleData(current);
    }

    private void ApplyRoleData(PlayerRoleType roleType)
    {
        if (roleDatabase == null)
        {
            Debug.LogError("[NetworkPlayerController] No se asignó RoleDatabaseSO en el Inspector del prefab!", this);
            return;
        }

        activeRole = roleDatabase.GetRole(roleType);

        if (activeRole == null)
        {
            Debug.LogError($"[NetworkPlayerController] No se encontró la data del rol {roleType} en RoleDatabase!", this);
            return;
        }

        if (IsServer)
        {
            NetworkHealth health = GetComponent<NetworkHealth>();
            if (health != null)
            {
                health.SetMaxStatsServer(activeRole.maxHealth, activeRole.maxArmor);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleLook();
        HandleMovement();
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnSprint(InputValue value) => isSprinting = value.isPressed;
    public void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded)
            jumpRequested = true;
    }

    private void HandleLook()
    {
        float yaw = lookInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up * yaw);

        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -upDownLookLimit, upDownLookLimit);
        cameraRoot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (activeRole == null) return;

        if (IsGrounded && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;

        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = isSprinting ? activeRole.sprintSpeed : activeRole.walkSpeed;
        characterController.Move(moveDir * currentSpeed * Time.deltaTime);

        if (jumpRequested && IsGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(activeRole.jumpForce * -2f * gravity);
            jumpRequested = false;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }
}