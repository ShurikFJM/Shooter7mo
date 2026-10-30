using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _customGameButton;
    [SerializeField] private Button _searchAndDestroyButton;
    [SerializeField] private Button _hardPointButton;
    [SerializeField] private Button _tutorialButton;

    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _controlsButton;
    [SerializeField] private Button _quitButton;

    [SerializeField] private GameObject _mainMenuRootPanel;
    [SerializeField] private GameObject _customGameConnectionPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _controlsPanel;

    [SerializeField] private string _tutorialScene;

    [SerializeField] private Button _backSettingsButton;
    [SerializeField] private Button _backControlsButton;

    private void Awake()
    {
        if (_customGameButton != null) _customGameButton.onClick.AddListener(OpenCustomGameConnection);
        if (_searchAndDestroyButton != null) _searchAndDestroyButton.onClick.AddListener(OnMatchmakingComingSoon);
        if (_hardPointButton != null) _hardPointButton.onClick.AddListener(OnMatchmakingComingSoon);
        if (_tutorialButton != null) _tutorialButton.onClick.AddListener(LoadTutorialScene);

        if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
        if (_controlsButton != null) _controlsButton.onClick.AddListener(OpenControls);
        if (_quitButton != null) _quitButton.onClick.AddListener(QuitGame);

        if (_backSettingsButton != null) _backSettingsButton.onClick.AddListener(CloseSettings);
        if (_backControlsButton != null) _backControlsButton.onClick.AddListener(CloseControls);

        if (_customGameConnectionPanel != null) _customGameConnectionPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
    }

    public void OpenCustomGameConnection()
    {
        if (_mainMenuRootPanel != null) _mainMenuRootPanel.SetActive(false);
        if (_customGameConnectionPanel != null) _customGameConnectionPanel.SetActive(true);
    }

    public void CloseCustomGameConnection()
    {
        if (_customGameConnectionPanel != null) _customGameConnectionPanel.SetActive(false);
        if (_mainMenuRootPanel != null) _mainMenuRootPanel.SetActive(true);
    }

    private void OnMatchmakingComingSoon()
    {
        Debug.Log("[Matchmaking] Modo en desarrollo. Usa Custom Game para partidas privadas.");
    }

    private void LoadTutorialScene()
    {
        if (!string.IsNullOrEmpty(_tutorialScene))
        {
            SceneManager.LoadSceneAsync(_tutorialScene);
        }
    }

    private void OpenSettings()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }

    private void OpenControls()
    {
        if (_controlsPanel != null) _controlsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    public void CloseControls()
    {
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}