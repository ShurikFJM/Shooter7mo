using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Paneles UI")]
    [Tooltip("Panel contenedor de todo el menú (el fondo oscuro completo)")]
    [SerializeField] private GameObject pauseMenuRoot;
    [Tooltip("Subpanel con los botones de Reanudar, Ajustes, etc.")]
    [SerializeField] private GameObject mainPausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Botones Menú Principal de Pausa")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Botones de Regreso en Subpaneles")]
    [SerializeField] private Button closeControlsButton;
    [SerializeField] private Button closeSettingsButton;

    [Header("Visualización de Controles Dinámicos")]
    [SerializeField] private GameObject keyboardControlsView;
    [SerializeField] private GameObject gamepadControlsView;
    [SerializeField] private TMP_Text currentDeviceText;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (controlsButton != null) controlsButton.onClick.AddListener(OpenControls);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (closeControlsButton != null) closeControlsButton.onClick.AddListener(ShowMainPausePanel);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(ShowMainPausePanel);

        // Ocultar todo al iniciar la partida
        ForceHideAll();
    }

    private void Update()
    {
        bool pausePressed = false;

        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
            pausePressed = true;
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            pausePressed = true;

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            pausePressed = true;
        else if (Input.GetKeyDown(KeyCode.JoystickButton7))
            pausePressed = true;

        if (pausePressed)
        {
            TogglePause();
        }

        if (isPaused && controlsPanel != null && controlsPanel.activeSelf)
        {
            UpdateControlsDisplay();
        }
    }

    public void TogglePause()
    {
        SetPauseState(!isPaused);
    }

    public void ResumeGame()
    {
        SetPauseState(false);
    }

    private void SetPauseState(bool state)
    {
        isPaused = state;

        if (isPaused)
        {
            // Encender el contenedor raíz y mostrar el panel principal de botones
            if (pauseMenuRoot != null) pauseMenuRoot.SetActive(true);
            ShowMainPausePanel();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ForceHideAll();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        UpdateLocalPlayerInputState(!isPaused);
    }

    private void ForceHideAll()
    {
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        if (mainPausePanel != null) mainPausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void ShowMainPausePanel()
    {
        if (mainPausePanel != null) mainPausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    private void OpenControls()
    {
        if (mainPausePanel != null) mainPausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        UpdateControlsDisplay();
    }

    private void OpenSettings()
    {
        if (mainPausePanel != null) mainPausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void UpdateControlsDisplay()
    {
        bool usingGamepad = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;

        if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) usingGamepad = false;
        if (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f || Mouse.current.leftButton.wasPressedThisFrame)) usingGamepad = false;

        if (keyboardControlsView != null) keyboardControlsView.SetActive(!usingGamepad);
        if (gamepadControlsView != null) gamepadControlsView.SetActive(usingGamepad);

        if (currentDeviceText != null)
        {
            currentDeviceText.text = usingGamepad ? "Dispositivo: Mando / Gamepad" : "Dispositivo: Teclado y Ratón";
        }
    }

    private void UpdateLocalPlayerInputState(bool enableInput)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
        {
            PlayerInput pInput = localPlayer.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                if (enableInput) pInput.ActivateInput();
                else pInput.DeactivateInput();
            }
        }
    }

    public void ReturnToMainMenu()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu");
    }
}