using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _controlsPanel;

    [SerializeField] private Button _playButton;
    [SerializeField] private Button _tutorialButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _controlsButton;
    [SerializeField] private Button _quitButton;

    [SerializeField] private string _gameScene;
    [SerializeField] private string _lobbyScene;
    [SerializeField] private string _tutorialScene;

    [SerializeField] private Button _backSettingsButton;
    [SerializeField] private Button _backControlsButton;

    private void Awake()
    {
        if (_playButton != null) _playButton.onClick.AddListener(LoadGameScene);
        if (_tutorialButton != null) _tutorialButton.onClick.AddListener(LoadTutorialScene);
        if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
        if (_controlsButton != null) _controlsButton.onClick.AddListener(OpenControls);
        if (_quitButton != null) _quitButton.onClick.AddListener(QuitGame);

        if (_backSettingsButton != null) _backSettingsButton.onClick.AddListener(CloseSettings);
        if (_backControlsButton != null) _backControlsButton.onClick.AddListener(CloseControls);

        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
    }

    private void LoadGameScene()
    {
        SceneManager.LoadSceneAsync(_gameScene);
    }

    private void LoadLobbyScene()
    {
        SceneManager.LoadSceneAsync(_lobbyScene);
    }

    private void LoadTutorialScene()
    {
        SceneManager.LoadSceneAsync(_tutorialScene);
    }

    private void OpenSettings()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }

    private void OpenControls()
    {
        if (_controlsPanel != null) _controlsPanel.SetActive(true);
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    public void CloseSettings()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    public void CloseControls()
    {
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
    }
}