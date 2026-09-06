using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f; // Gravedad firme al estilo CS

    [Header("Cámara")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxPitch = 85f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Bloquear y ocultar el cursor en pantalla
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotación vertical (Cámara)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxPitch, maxPitch);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (Cuerpo del jugador)
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        // Mantener al jugador pegado al suelo cuando camina
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Entradas de movimiento (WASD / Flechas)
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Aplicar gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

    }
    public float CurrentHorizontalSpeed => new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
}