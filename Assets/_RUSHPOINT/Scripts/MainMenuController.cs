using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _customGameButton;
    [SerializeField] private Button _searchAndDestroyButton;
    [SerializeField] private Button _hardPointButton;
    [SerializeField] private Button _tutorialButton;
    [SerializeField] private Button _creditsButton;

    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _generalSettingsButton;
    [SerializeField] private Button _audioSettingsButton;
    [SerializeField] private Button _controlsButton;
    [SerializeField] private Button _quitButton;

    [SerializeField] private GameObject _mainMenuRootPanel;
    [SerializeField] private GameObject _customGameConnectionPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _generalSettingsPanel;
    [SerializeField] private GameObject _audioSettingsPanel;
    [SerializeField] private GameObject _controlsPanel;
    [SerializeField] private GameObject _creditsPanel;

    [SerializeField] private Button _backSettingsButton;
    [SerializeField] private Button _backAudioSettingsButton;
    [SerializeField] private Button _backControlsButton;
    [SerializeField] private Button _backCreditsButton;

    [SerializeField] private string _tutorialScene;

    private void Awake()
    {
        if (_customGameButton != null) _customGameButton.onClick.AddListener(OpenCustomGameConnection);
        if (_searchAndDestroyButton != null) _searchAndDestroyButton.onClick.AddListener(OnMatchmakingComingSoon);
        if (_hardPointButton != null) _hardPointButton.onClick.AddListener(OnMatchmakingComingSoon);
        if (_tutorialButton != null) _tutorialButton.onClick.AddListener(LoadTutorialScene);
        if (_creditsButton != null) _creditsButton.onClick.AddListener(OpenCredits);

        if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
        if (_generalSettingsButton != null) _generalSettingsButton.onClick.AddListener(OpenGeneralSettings);
        if (_audioSettingsButton != null) _audioSettingsButton.onClick.AddListener(OpenAudioSettings);
        if (_controlsButton != null) _controlsButton.onClick.AddListener(OpenControls);
        if (_quitButton != null) _quitButton.onClick.AddListener(QuitGame);

        if (_backSettingsButton != null) _backSettingsButton.onClick.AddListener(CloseSettings);
        if (_backAudioSettingsButton != null) _backAudioSettingsButton.onClick.AddListener(CloseAudioSettings);
        if (_backControlsButton != null) _backControlsButton.onClick.AddListener(CloseControls);
        if (_backCreditsButton != null) _backCreditsButton.onClick.AddListener(CloseCredits);

        ShowExclusivePanel(_mainMenuRootPanel, _customGameButton != null ? _customGameButton.gameObject : null);
    }

    private void Start()
    {
        SelectUIElement(_customGameButton != null ? _customGameButton.gameObject : null);
    }

    private void ShowExclusivePanel(GameObject targetPanel, GameObject preferredFocusElement = null)
    {
        if (_mainMenuRootPanel != null) _mainMenuRootPanel.SetActive(false);
        if (_customGameConnectionPanel != null) _customGameConnectionPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_creditsPanel != null) _creditsPanel.SetActive(false);

        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            SelectFirstInPanel(targetPanel, preferredFocusElement);
        }
    }

    private void ShowSettingsSubpanel(GameObject targetSubpanel, GameObject preferredFocusElement = null)
    {
        if (_settingsPanel != null && !_settingsPanel.activeSelf)
        {
            ShowExclusivePanel(_settingsPanel);
        }

        if (_generalSettingsPanel != null) _generalSettingsPanel.SetActive(false);
        if (_audioSettingsPanel != null) _audioSettingsPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);

        if (targetSubpanel != null)
        {
            targetSubpanel.SetActive(true);
        }

        if (preferredFocusElement != null && preferredFocusElement.activeInHierarchy)
        {
            SelectUIElement(preferredFocusElement);
        }
    }

    public void OpenCredits()
    {
        ShowExclusivePanel(_creditsPanel, _backCreditsButton != null ? _backCreditsButton.gameObject : null);
    }

    public void CloseCredits()
    {
        ShowExclusivePanel(_mainMenuRootPanel, _creditsButton != null ? _creditsButton.gameObject : null);
    }

    public void OpenCustomGameConnection()
    {
        ShowExclusivePanel(_customGameConnectionPanel);
    }

    public void CloseCustomGameConnection()
    {
        ShowExclusivePanel(_mainMenuRootPanel, _customGameButton != null ? _customGameButton.gameObject : null);
    }

    public void OpenSettings()
    {
        ShowExclusivePanel(_settingsPanel);
        OpenGeneralSettings();
    }

    public void CloseSettings()
    {
        ShowExclusivePanel(_mainMenuRootPanel, _settingsButton != null ? _settingsButton.gameObject : null);
    }

    public void OpenGeneralSettings()
    {
        ShowSettingsSubpanel(_generalSettingsPanel, _generalSettingsButton != null ? _generalSettingsButton.gameObject : null);
    }

    public void OpenAudioSettings()
    {
        ShowSettingsSubpanel(_audioSettingsPanel, _audioSettingsButton != null ? _audioSettingsButton.gameObject : null);
    }

    public void CloseAudioSettings()
    {
        CloseSettings();
    }

    public void OpenControls()
    {
        ShowSettingsSubpanel(_controlsPanel, _controlsButton != null ? _controlsButton.gameObject : null);
    }

    public void CloseControls()
    {
        CloseSettings();
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

    private void QuitGame()
    {
        Application.Quit();
    }

    private void SelectFirstInPanel(GameObject panel, GameObject preferredElement = null)
    {
        if (panel == null) return;

        if (preferredElement != null && preferredElement.activeInHierarchy)
        {
            SelectUIElement(preferredElement);
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
    }

    public void SelectUIElement(GameObject target)
    {
        if (EventSystem.current == null || target == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }
}