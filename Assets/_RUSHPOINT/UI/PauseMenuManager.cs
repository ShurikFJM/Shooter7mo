using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    public static PauseMenuManager Instance { get; private set; }

    [SerializeField] private GameObject _pauseMenuRoot;
    [SerializeField] private GameObject _mainPausePanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _controlsPanel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _controlsButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _closeControlsButton;
    [SerializeField] private Button _closeSettingsButton;

    private InputAction _pauseAction;
    private bool _isPaused;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ConfigureInputActions();
    }

    private void Start()
    {
        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
        if (_controlsButton != null) _controlsButton.onClick.AddListener(OpenControls);
        if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
        if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (_closeControlsButton != null) _closeControlsButton.onClick.AddListener(ShowMainPausePanel);
        if (_closeSettingsButton != null) _closeSettingsButton.onClick.AddListener(ShowMainPausePanel);

        ForceHideAll();
    }

    private void OnEnable()
    {
        if (_pauseAction == null)
        {
            ConfigureInputActions();
        }

        _pauseAction.performed += HandlePausePerformed;
        _pauseAction.Enable();
    }

    private void OnDisable()
    {
        if (_pauseAction != null)
        {
            _pauseAction.performed -= HandlePausePerformed;
            _pauseAction.Disable();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _pauseAction?.Dispose();
    }

    private void ConfigureInputActions()
    {
        _pauseAction = new InputAction(name: "TogglePause", type: InputActionType.Button);
        _pauseAction.AddBinding("<Keyboard>/escape");
        _pauseAction.AddBinding("<Keyboard>/p");
        _pauseAction.AddBinding("<Gamepad>/start");
    }

    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            return;
        }

        if (_isPaused)
        {
            if ((_controlsPanel != null && _controlsPanel.activeSelf) ||
                (_settingsPanel != null && _settingsPanel.activeSelf))
            {
                ShowMainPausePanel();
                return;
            }
        }

        TogglePause();
    }

    public void TogglePause()
    {
        SetPauseState(!_isPaused);
    }

    public void ResumeGame()
    {
        SetPauseState(false);
    }

    public void PauseGame()
    {
        SetPauseState(true);
    }

    private void SetPauseState(bool state)
    {
        _isPaused = state;

        if (_isPaused)
        {
            if (_pauseMenuRoot != null) _pauseMenuRoot.SetActive(true);
            ShowMainPausePanel();
            CursorStateManager.Instance?.RegisterCursorUnlockRequester();
        }
        else
        {
            ForceHideAll();
            CursorStateManager.Instance?.UnregisterCursorUnlockRequester();
        }

        UpdateLocalPlayerInputState(!_isPaused);
    }

    private bool ShouldKeepCursorUnlocked()
    {
        if (RoleSelectScreenUI.instance != null && RoleSelectScreenUI.instance.isRoleSelectionActive)
        {
            return true;
        }

        if (RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value == RoundPhase.WaitingForPlayers)
        {
            return true;
        }

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            return true;
        }

        return false;
    }

    private void ForceHideAll()
    {
        if (_pauseMenuRoot != null) _pauseMenuRoot.SetActive(false);
        if (_mainPausePanel != null) _mainPausePanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    public void ShowMainPausePanel()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(true);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);

        SelectUIElement(_resumeButton != null ? _resumeButton.gameObject : null);
    }

    private void OpenControls()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(true);

        SelectFirstSelectableInPanel(_controlsPanel, _closeControlsButton != null ? _closeControlsButton.gameObject : null);
    }

    private void OpenSettings()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(true);

        SelectFirstSelectableInPanel(_settingsPanel, _closeSettingsButton != null ? _closeSettingsButton.gameObject : null);
    }

    private void UpdateLocalPlayerInputState(bool enableInput)
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetGameplayInputActive(enableInput);
        }

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null) return;

        NetworkPlayerController playerController = localPlayer.GetComponent<NetworkPlayerController>();
        if (playerController != null)
        {
            bool keepUnlocked = !enableInput || ShouldKeepCursorUnlocked();
            playerController.SetCursorLocked(!keepUnlocked);
        }
    }

    private void SelectFirstSelectableInPanel(GameObject panel, GameObject fallbackElement)
    {
        if (panel == null)
        {
            SelectUIElement(fallbackElement);
            return;
        }

        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null && selectables[i].gameObject.activeInHierarchy && selectables[i].interactable)
            {
                SelectUIElement(selectables[i].gameObject);
                return;
            }
        }

        SelectUIElement(fallbackElement);
    }

    public void SelectUIElement(GameObject target)
    {
        if (EventSystem.current == null || target == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(DisconnectAndLoadMenuRoutine());
    }

    private IEnumerator DisconnectAndLoadMenuRoutine()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            while (NetworkManager.Singleton.ShutdownInProgress)
            {
                yield return null;
            }

            Destroy(NetworkManager.Singleton.gameObject);
        }

        SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
    }
}