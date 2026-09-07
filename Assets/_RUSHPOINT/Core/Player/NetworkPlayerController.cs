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
    [SerializeField] private RoleDataSO activeRole;

    private float gravity = -19.62f;
    private Vector3 verticalVelocity;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool jumpRequested;
    private float cameraPitch = 0f;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            playerCamera.gameObject.SetActive(true);
            audioListener.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            playerCamera.gameObject.SetActive(false);
            audioListener.enabled = false;
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
        if (value.isPressed && characterController.isGrounded)
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

        bool isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;

        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = isSprinting ? activeRole.sprintSpeed : activeRole.walkSpeed;
        characterController.Move(moveDir * currentSpeed * Time.deltaTime);

        if (jumpRequested && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(activeRole.jumpForce * -2f * gravity);
            jumpRequested = false;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }
}