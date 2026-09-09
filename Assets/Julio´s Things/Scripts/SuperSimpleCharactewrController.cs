using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 6f;
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Sensibilidad de Cámara")]
    public float mouseSensitivity = 2f;
    public float controllerSensitivity = 120f; // Sensibilidad independiente para el Stick Derecho

    [Header("Referencias")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsMoving { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCameraLook();
        HandleMovement();
    }

    void HandleCameraLook()
    {
        // 1. Entrada de Ratón
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 2. Entrada de Stick Derecho de Xbox
        float joystickX = 0f;
        float joystickY = 0f;

        try { joystickX = Input.GetAxis("RightStickX"); } catch { }
        try { joystickY = Input.GetAxis("RightStickY"); } catch { }

        // Fallback por defecto en Unity Legacy Input
        if (Mathf.Approximately(joystickX, 0f)) try { joystickX = Input.GetAxis("JoystickAxis4"); } catch { }
        if (Mathf.Approximately(joystickY, 0f)) try { joystickY = Input.GetAxis("JoystickAxis5"); } catch { }

        float finalLookX = mouseX + (joystickX * controllerSensitivity * Time.deltaTime);
        float finalLookY = mouseY + (joystickY * controllerSensitivity * Time.deltaTime);

        // Rotación Vertical (Inclinación)
        xRotation -= finalLookY;
        xRotation = Mathf.Clamp(xRotation, -89f, 89f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación Horizontal (Cuerpo)
        transform.Rotate(Vector3.up * finalLookX);
    }

    void HandleMovement()
    {
        // Soporta tanto WASD como Stick Izquierdo automáticamente
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        controller.Move(moveDirection * walkSpeed * Time.deltaTime);

        IsMoving = moveDirection.sqrMagnitude > 0.01f;

        // Gravedad y Salto (Espacio O Botón 'A' de Xbox)
        if (IsGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        bool jumpInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);

        if (jumpInput && IsGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}