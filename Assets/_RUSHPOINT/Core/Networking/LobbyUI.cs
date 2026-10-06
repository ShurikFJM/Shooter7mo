using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private TMP_Text _joinCodeDisplayText;
    [SerializeField] private Button _copyCodeButton;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _roleSelectCanvas;

    private string _activeJoinCode;

    private void Start()
    {
        if (_roleSelectCanvas != null)
        {
            _roleSelectCanvas.SetActive(false);
        }

        if (_hostButton != null)
        {
            _hostButton.onClick.AddListener(OnHostClicked);
        }

        if (_joinButton != null)
        {
            _joinButton.onClick.AddListener(OnJoinClicked);
        }

        if (_copyCodeButton != null)
        {
            _copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
            _copyCodeButton.gameObject.SetActive(false);
        }

        if (RelayNetworkManager.Instance != null)
        {
            RelayNetworkManager.Instance.OnHostStarted += HandleHostStarted;
            RelayNetworkManager.Instance.OnClientConnected += HandleClientConnected;
            RelayNetworkManager.Instance.OnConnectionFailed += HandleConnectionFailed;
        }
    }

    private void OnDestroy()
    {
        if (_hostButton != null) _hostButton.onClick.RemoveListener(OnHostClicked);
        if (_joinButton != null) _joinButton.onClick.RemoveListener(OnJoinClicked);
        if (_copyCodeButton != null) _copyCodeButton.onClick.RemoveListener(OnCopyCodeClicked);

        if (RelayNetworkManager.Instance != null)
        {
            RelayNetworkManager.Instance.OnHostStarted -= HandleHostStarted;
            RelayNetworkManager.Instance.OnClientConnected -= HandleClientConnected;
            RelayNetworkManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
        }
    }

    private async void OnHostClicked()
    {
        SetButtonsInteractable(false);
        string code = await RelayNetworkManager.Instance.StartHostWithRelayAsync();
        if (string.IsNullOrEmpty(code))
        {
            SetButtonsInteractable(true);
        }
    }

    private async void OnJoinClicked()
    {
        if (_joinCodeInputField == null) return;

        string code = _joinCodeInputField.text;
        SetButtonsInteractable(false);

        bool success = await RelayNetworkManager.Instance.StartClientWithRelayAsync(code);
        if (!success)
        {
            SetButtonsInteractable(true);
        }
    }

    private void OnCopyCodeClicked()
    {
        if (string.IsNullOrEmpty(_activeJoinCode)) return;

        GUIUtility.systemCopyBuffer = _activeJoinCode;
        if (_joinCodeDisplayText != null)
        {
            _joinCodeDisplayText.text = $"CODE: {_activeJoinCode} (COPIED!)";
        }
    }

    private void HandleHostStarted(string joinCode)
    {
        _activeJoinCode = joinCode;
        GUIUtility.systemCopyBuffer = joinCode;

        if (_joinCodeDisplayText != null)
        {
            _joinCodeDisplayText.text = $"CODE: {joinCode}";
            _joinCodeDisplayText.gameObject.SetActive(true);
        }

        if (_copyCodeButton != null)
        {
            _copyCodeButton.gameObject.SetActive(true);
        }

        TransitionToRoleSelect();
    }

    private void HandleClientConnected()
    {
        TransitionToRoleSelect();
    }

    private void TransitionToRoleSelect()
    {
        if (_lobbyPanel != null)
        {
            _lobbyPanel.SetActive(false);
        }

        if (_roleSelectCanvas != null)
        {
            _roleSelectCanvas.SetActive(true);
        }
    }

    private void HandleConnectionFailed(string error)
    {
        SetButtonsInteractable(true);
        Debug.LogError($"[Relay] Error: {error}");
    }

    private void SetButtonsInteractable(bool state)
    {
        if (_hostButton != null) _hostButton.interactable = state;
        if (_joinButton != null) _joinButton.interactable = state;
    }
}