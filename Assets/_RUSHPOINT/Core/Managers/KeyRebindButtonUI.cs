using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyRebindButtonUI : MonoBehaviour
{
    private const string _WAITING_INPUT_TEXT = "Press any key...";

    [SerializeField] private InputActionAsset _inputActionAsset;
    [SerializeField] private string _actionMapName = "Player";
    [SerializeField] private string _actionName;
    [SerializeField] private int _bindingIndex = 0;

    [SerializeField] private Button _rebindButton;
    [SerializeField] private TMP_Text _bindingDisplayNameText;

    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private InputAction _targetAction;

    private void Awake()
    {
        if (_rebindButton != null)
        {
            _rebindButton.onClick.AddListener(StartRebindingProcess);
        }
    }

    private void Start()
    {
        InitializeActionReference();
        UpdateBindingDisplay();
    }

    private void InitializeActionReference()
    {
        if (_inputActionAsset == null) return;

        InputActionMap actionMap = _inputActionAsset.FindActionMap(_actionMapName);
        if (actionMap != null)
        {
            _targetAction = actionMap.FindAction(_actionName);
        }
    }

    public void UpdateBindingDisplay()
    {
        if (_targetAction == null)
        {
            InitializeActionReference();
        }

        if (_targetAction == null || _bindingDisplayNameText == null) return;

        _bindingDisplayNameText.text = _targetAction.GetBindingDisplayString(_bindingIndex);
    }

    public void StartRebindingProcess()
    {
        if (_targetAction == null) return;

        _targetAction.Disable();

        if (_bindingDisplayNameText != null)
        {
            _bindingDisplayNameText.text = _WAITING_INPUT_TEXT;
        }

        _rebindingOperation?.Cancel();

        _rebindingOperation = _targetAction.PerformInteractiveRebinding(_bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => CompleteRebindingProcess())
            .OnCancel(operation => CancelRebindingProcess());

        _rebindingOperation.Start();
    }

    private void CompleteRebindingProcess()
    {
        _rebindingOperation?.Dispose();
        _rebindingOperation = null;

        _targetAction.Enable();
        UpdateBindingDisplay();

        if (KeyRebindManager.Instance != null)
        {
            KeyRebindManager.Instance.SaveCustomBindings();
        }
    }

    private void CancelRebindingProcess()
    {
        _rebindingOperation?.Dispose();
        _rebindingOperation = null;

        _targetAction.Enable();
        UpdateBindingDisplay();
    }

    private void OnDestroy()
    {
        _rebindingOperation?.Dispose();
    }
}