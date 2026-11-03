using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindMenuController : MonoBehaviour
{
    private const string KEYBOARD_DEVICE_STRING = "Current Device: Keyboard and Mouse";
    private const string GAMEPAD_DEVICE_STRING = "Current Device: Controller (Xbox)";
    private const string MOUSE_SENSITIVITY_FORMAT = "Mouse Sens: {0:F2}";
    private const string GAMEPAD_SENSITIVITY_FORMAT = "Controller Sens: {0:F0}";

    [SerializeField] private TMP_Text _currentDeviceText;
    [SerializeField] private GameObject _keyboardControlsView;
    [SerializeField] private GameObject _gamepadControlsView;

    [SerializeField] private Button _keyboardTabButton;
    [SerializeField] private Button _gamepadTabButton;
    [SerializeField] private Button _resetDefaultsButton;

    [SerializeField] private Slider _mouseSensitivitySlider;
    [SerializeField] private TMP_Text _mouseSensitivityText;
    [SerializeField] private Slider _gamepadSensitivitySlider;
    [SerializeField] private TMP_Text _gamepadSensitivityText;

    private void Start()
    {
        if (_keyboardTabButton != null) _keyboardTabButton.onClick.AddListener(ShowKeyboardTab);
        if (_gamepadTabButton != null) _gamepadTabButton.onClick.AddListener(ShowGamepadTab);
        if (_resetDefaultsButton != null) _resetDefaultsButton.onClick.AddListener(ResetAllBindings);

        if (_mouseSensitivitySlider != null)
        {
            _mouseSensitivitySlider.minValue = 0.1f;
            _mouseSensitivitySlider.maxValue = 5.0f;
            _mouseSensitivitySlider.value = InputManager.Instance != null ? InputManager.Instance.MouseSensitivity : 1.0f;
            _mouseSensitivitySlider.onValueChanged.AddListener(HandleMouseSensitivityChanged);
            UpdateMouseSensitivityLabel(_mouseSensitivitySlider.value);
        }

        if (_gamepadSensitivitySlider != null)
        {
            _gamepadSensitivitySlider.minValue = 10f;
            _gamepadSensitivitySlider.maxValue = 250f;
            _gamepadSensitivitySlider.value = InputManager.Instance != null ? InputManager.Instance.GamepadSensitivity : 75.0f;
            _gamepadSensitivitySlider.onValueChanged.AddListener(HandleGamepadSensitivityChanged);
            UpdateGamepadSensitivityLabel(_gamepadSensitivitySlider.value);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDeviceChanged += HandleInputDeviceAutoSwitch;
            SelectDeviceView(InputManager.Instance.IsGamepadActive);
        }
        else
        {
            ShowKeyboardTab();
        }
    }

    private void OnDestroy()
    {
        if (_keyboardTabButton != null) _keyboardTabButton.onClick.RemoveListener(ShowKeyboardTab);
        if (_gamepadTabButton != null) _gamepadTabButton.onClick.RemoveListener(ShowGamepadTab);
        if (_resetDefaultsButton != null) _resetDefaultsButton.onClick.RemoveListener(ResetAllBindings);

        if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.onValueChanged.RemoveListener(HandleMouseSensitivityChanged);
        if (_gamepadSensitivitySlider != null) _gamepadSensitivitySlider.onValueChanged.RemoveListener(HandleGamepadSensitivityChanged);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDeviceChanged -= HandleInputDeviceAutoSwitch;
        }
    }

    private void HandleInputDeviceAutoSwitch(bool isGamepad)
    {
        SelectDeviceView(isGamepad);
    }

    private void SelectDeviceView(bool isGamepad)
    {
        if (isGamepad)
        {
            ShowGamepadTab();
        }
        else
        {
            ShowKeyboardTab();
        }

        if (_currentDeviceText != null)
        {
            _currentDeviceText.text = isGamepad ? GAMEPAD_DEVICE_STRING : KEYBOARD_DEVICE_STRING;
        }
    }

    public void ShowKeyboardTab()
    {
        if (_keyboardControlsView != null) _keyboardControlsView.SetActive(true);
        if (_gamepadControlsView != null) _gamepadControlsView.SetActive(false);
    }

    public void ShowGamepadTab()
    {
        if (_keyboardControlsView != null) _keyboardControlsView.SetActive(false);
        if (_gamepadControlsView != null) _gamepadControlsView.SetActive(true);
    }

    public void ResetAllBindings()
    {
        if (InputManager.Instance == null) return;

        InputManager.Instance.ResetAllBindingsToDefault();

        RebindActionUI[] actionRows = GetComponentsInChildren<RebindActionUI>(true);
        for (int i = 0; i < actionRows.Length; i++)
        {
            actionRows[i].RefreshDisplay();
        }
    }

    private void HandleMouseSensitivityChanged(float value)
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.MouseSensitivity = value;
        }
        UpdateMouseSensitivityLabel(value);
    }

    private void HandleGamepadSensitivityChanged(float value)
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.GamepadSensitivity = value;
        }
        UpdateGamepadSensitivityLabel(value);
    }

    private void UpdateMouseSensitivityLabel(float value)
    {
        if (_mouseSensitivityText != null)
        {
            _mouseSensitivityText.text = string.Format(MOUSE_SENSITIVITY_FORMAT, value);
        }
    }

    private void UpdateGamepadSensitivityLabel(float value)
    {
        if (_gamepadSensitivityText != null)
        {
            _gamepadSensitivityText.text = string.Format(GAMEPAD_SENSITIVITY_FORMAT, value);
        }
    }
}