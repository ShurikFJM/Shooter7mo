using UnityEngine;
using UnityEngine.UI;

public class LocalizationMenu : MonoBehaviour
{
    [SerializeField] private Canvas _localizationCanvas;
    [SerializeField] private Canvas _menuCanvas;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _backButton;

    private void Start()
    {
        _openButton.onClick.AddListener(OpenLocalizationCanvas);
        _backButton.onClick.AddListener(CloseLocalizationCanvas);
    }

    private void OpenLocalizationCanvas()
    {
        _localizationCanvas.enabled = true;
        _menuCanvas.enabled = false;
    }

    private void CloseLocalizationCanvas()
    {
        _localizationCanvas.enabled = false;
        _menuCanvas.enabled = true;
    }
}
