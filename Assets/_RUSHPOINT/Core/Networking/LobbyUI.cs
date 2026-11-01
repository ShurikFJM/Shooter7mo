using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    private const string PLAYER_PREFS_NAME_KEY = "Rushpoint_PlayerName";
    private const string DEFAULT_PLAYER_PREFIX = "Player_";

    [SerializeField] private TMP_InputField _playerNameInputField;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private Button _backButton;
    [SerializeField] private GameObject _connectionPanel;
    [SerializeField] private GameObject _customLobbyCanvas;
    [SerializeField] private GameObject _mainMenuRootPanel;

    public static string LocalPlayerName { get; private set; }

    private void Start()
    {
        string savedName = PlayerPrefs.GetString(PLAYER_PREFS_NAME_KEY, DEFAULT_PLAYER_PREFIX + Random.Range(100, 999));
        LocalPlayerName = savedName;

        if (_playerNameInputField != null)
        {
            _playerNameInputField.text = savedName;
            _playerNameInputField.onValueChanged.AddListener(OnPlayerNameChanged);
        }

        if (_customLobbyCanvas != null)
        {
            _customLobbyCanvas.SetActive(false);
        }

        if (_hostButton != null) _hostButton.onClick.AddListener(OnHostClicked);
        if (_joinButton != null) _joinButton.onClick.AddListener(OnJoinClicked);
        if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

        if (RelayNetworkManager.Instance != null)
        {
            RelayNetworkManager.Instance.OnHostStarted += HandleHostStarted;
            RelayNetworkManager.Instance.OnClientConnected += HandleClientConnected;
            RelayNetworkManager.Instance.OnConnectionFailed += HandleConnectionFailed;
        }
    }

    private void OnDestroy()
    {
        if (_playerNameInputField != null) _playerNameInputField.onValueChanged.RemoveListener(OnPlayerNameChanged);
        if (_hostButton != null) _hostButton.onClick.RemoveListener(OnHostClicked);
        if (_joinButton != null) _joinButton.onClick.RemoveListener(OnJoinClicked);
        if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);

        if (RelayNetworkManager.Instance != null)
        {
            RelayNetworkManager.Instance.OnHostStarted -= HandleHostStarted;
            RelayNetworkManager.Instance.OnClientConnected -= HandleClientConnected;
            RelayNetworkManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
        }
    }

    private void OnPlayerNameChanged(string newName)
    {
        string sanitized = string.IsNullOrWhiteSpace(newName) ? DEFAULT_PLAYER_PREFIX + Random.Range(100, 999) : newName.Trim();
        LocalPlayerName = sanitized;
        PlayerPrefs.SetString(PLAYER_PREFS_NAME_KEY, sanitized);
        PlayerPrefs.Save();
    }

    private async void OnHostClicked()
    {
        ValidatePlayerNameInput();

        if (RelayNetworkManager.Instance == null)
        {
            Debug.LogError("[LobbyUI] Falta RelayNetworkManager en la escena.");
            return;
        }

        SetButtonsInteractable(false);

        try
        {
            string code = await RelayNetworkManager.Instance.StartHostWithRelayAsync();
            if (!string.IsNullOrEmpty(code))
            {
                HandleHostStarted(code);
            }
            else
            {
                SetButtonsInteractable(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Relay] Error al hostear: {ex.Message}");
            SetButtonsInteractable(true);
        }
    }

    private async void OnJoinClicked()
    {
        if (_joinCodeInputField == null) return;

        ValidatePlayerNameInput();
        string code = _joinCodeInputField.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[LobbyUI] Codigo vacio.");
            return;
        }

        if (RelayNetworkManager.Instance == null)
        {
            Debug.LogError("[LobbyUI] Falta RelayNetworkManager en la escena.");
            return;
        }

        SetButtonsInteractable(false);

        try
        {
            bool success = await RelayNetworkManager.Instance.StartClientWithRelayAsync(code);
            if (success)
            {
                TransitionToCustomLobby();
            }
            else
            {
                SetButtonsInteractable(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Relay] Error al unirse: {ex.Message}");
            SetButtonsInteractable(true);
        }
    }

    private void OnBackClicked()
    {
        if (_connectionPanel != null) _connectionPanel.SetActive(false);
        if (_mainMenuRootPanel != null) _mainMenuRootPanel.SetActive(true);
    }

    private void ValidatePlayerNameInput()
    {
        if (_playerNameInputField != null)
        {
            if (string.IsNullOrWhiteSpace(_playerNameInputField.text))
            {
                OnPlayerNameChanged(DEFAULT_PLAYER_PREFIX + Random.Range(100, 999));
                _playerNameInputField.text = LocalPlayerName;
            }
            else
            {
                OnPlayerNameChanged(_playerNameInputField.text);
            }
        }
    }

    private void HandleHostStarted(string joinCode)
    {
        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.SetLobbyJoinCode(joinCode);
        }

        TransitionToCustomLobby();
    }

    private void HandleClientConnected()
    {
        TransitionToCustomLobby();
    }

    private void TransitionToCustomLobby()
    {
        if (_customLobbyCanvas == null)
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i].name == "Canvas_Lobby_CustomGame")
                {
                    _customLobbyCanvas = rootObjects[i];
                    break;
                }
            }
        }

        if (_connectionPanel != null) _connectionPanel.SetActive(false);
        if (_mainMenuRootPanel != null) _mainMenuRootPanel.SetActive(false);
        if (_customLobbyCanvas != null) _customLobbyCanvas.SetActive(true);

        SetButtonsInteractable(true);
    }

    private void HandleConnectionFailed(string error)
    {
        SetButtonsInteractable(true);
        Debug.LogError($"[Relay] Connection Error: {error}");
    }

    private void SetButtonsInteractable(bool state)
    {
        if (_hostButton != null) _hostButton.interactable = state;
        if (_joinButton != null) _joinButton.interactable = state;
    }
}