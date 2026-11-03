using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindActionUI : MonoBehaviour
{
    private const string LISTENING_TEXT = "Press Key / Button...";

    [SerializeField] private string _actionName = "Fire";
    [SerializeField] private bool _isGamepadBinding = false;
    [SerializeField] private string _compositePartName = "";

    [SerializeField] private TMP_Text _actionTitleText;
    [SerializeField] private TMP_Text _bindingDisplayText;
    [SerializeField] private Button _rebindButton;
    [SerializeField] private GameObject _listeningOverlay;

    private int _cachedBindingIndex = -1;

    public Button RebindButton => _rebindButton;
    public string ActionName => _actionName;
    public bool IsGamepadBinding => _isGamepadBinding;

    private void Start()
    {
        if (_rebindButton != null)
        {
            _rebindButton.onClick.AddListener(TriggerRebind);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBindingsRebound += RefreshDisplay;
            InputManager.Instance.OnInputDeviceChanged += HandleDeviceChanged;
        }

        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (_rebindButton != null)
        {
            _rebindButton.onClick.RemoveListener(TriggerRebind);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBindingsRebound -= RefreshDisplay;
            InputManager.Instance.OnInputDeviceChanged -= HandleDeviceChanged;
        }
    }

    public void SetTargetBinding(string actionName, bool isGamepad, string compositePart = "")
    {
        _actionName = actionName;
        _isGamepadBinding = isGamepad;
        _compositePartName = compositePart;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (InputManager.Instance == null) return;

        _cachedBindingIndex = InputManager.Instance.GetBindingIndex(_actionName, _isGamepadBinding, _compositePartName);

        if (_actionTitleText != null)
        {
            _actionTitleText.text = string.IsNullOrEmpty(_compositePartName)
                ? _actionName
                : string.Concat(_actionName, " (", _compositePartName, ")");
        }

        if (_bindingDisplayText != null)
        {
            string display = InputManager.Instance.GetBindingDisplayString(_actionName, _cachedBindingIndex);
            _bindingDisplayText.text = string.IsNullOrEmpty(display) ? "None" : display;
        }

        if (_listeningOverlay != null)
        {
            _listeningOverlay.SetActive(false);
        }
    }

    private void HandleDeviceChanged(bool isGamepad)
    {
        RefreshDisplay();
    }

    public void TriggerRebind()
    {
        if (InputManager.Instance == null) return;

        if (_cachedBindingIndex < 0)
        {
            _cachedBindingIndex = InputManager.Instance.GetBindingIndex(_actionName, _isGamepadBinding, _compositePartName);
        }

        if (_cachedBindingIndex < 0) return;

        if (_listeningOverlay != null)
        {
            _listeningOverlay.SetActive(true);
        }

        if (_bindingDisplayText != null)
        {
            _bindingDisplayText.text = LISTENING_TEXT;
        }

        InputManager.Instance.StartInteractiveRebind(
            _actionName,
            _cachedBindingIndex,
            HandleRebindComplete,
            HandleRebindCancel
        );
    }

    private void HandleRebindComplete(string newBindingPath)
    {
        if (_listeningOverlay != null)
        {
            _listeningOverlay.SetActive(false);
        }

        RefreshDisplay();
    }

    private void HandleRebindCancel()
    {
        if (_listeningOverlay != null)
        {
            _listeningOverlay.SetActive(false);
        }

        RefreshDisplay();
    }
}