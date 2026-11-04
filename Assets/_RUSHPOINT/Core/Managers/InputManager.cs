using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private const string BINDINGS_PREFS_KEY = "CustomInputBindings";
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string GAMEPAD_SENSITIVITY_KEY = "GamepadSensitivity";
    private const float DEFAULT_MOUSE_SENSITIVITY = 1.0f;
    private const float DEFAULT_GAMEPAD_SENSITIVITY = 75.0f;
    private const string ACTION_MAP_PLAYER = "Player";
    private const string ACTION_MOVE = "Move";
    private const string ACTION_LOOK = "Look";
    private const string ACTION_FIRE = "Fire";
    private const string ACTION_ATTACK_ALT = "Attack";
    private const string ACTION_AIM = "Aim";
    private const string ACTION_RELOAD = "Reload";
    private const string ACTION_INTERACT = "Interact";
    private const string ACTION_JUMP = "Jump";
    private const string ACTION_CROUCH = "Crouch";
    private const string ACTION_WALK = "Walk";
    private const string ACTION_DROP = "Drop";
    private const string ACTION_SLOT_1 = "Slot1";
    private const string ACTION_SLOT_2 = "Slot2";
    private const string ACTION_SLOT_3 = "Slot3";
    private const string ACTION_PAUSE = "Pause";
    private const string ACTION_SCOREBOARD = "Scoreboard";
    private const string ACTION_CHAT = "Chat";

    private static InputManager _instance;
    public static InputManager Instance => _instance;

    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private float _mouseSensitivity = DEFAULT_MOUSE_SENSITIVITY;
    [SerializeField] private float _gamepadSensitivity = DEFAULT_GAMEPAD_SENSITIVITY;

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _fireAction;
    private InputAction _aimAction;
    private InputAction _reloadAction;
    private InputAction _interactAction;
    private InputAction _jumpAction;
    private InputAction _crouchAction;
    private InputAction _walkAction;
    private InputAction _dropAction;
    private InputAction _slot1Action;
    private InputAction _slot2Action;
    private InputAction _slot3Action;
    private InputAction _pauseAction;
    private InputAction _scoreboardAction;
    private InputAction _chatAction;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    private bool _isGamepadActive;

    public event Action<bool> OnInputDeviceChanged;
    public event Action OnBindingsRebound;

    public bool IsGamepadActive => _isGamepadActive;
    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set
        {
            _mouseSensitivity = Mathf.Max(0.05f, value);
            PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, _mouseSensitivity);
            PlayerPrefs.Save();
        }
    }

    public float GamepadSensitivity
    {
        get => _gamepadSensitivity;
        set
        {
            _gamepadSensitivity = Mathf.Max(5f, value);
            PlayerPrefs.SetFloat(GAMEPAD_SENSITIVITY_KEY, _gamepadSensitivity);
            PlayerPrefs.Save();
        }
    }

    public Vector2 MovementInput => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

    public Vector2 LookInput
    {
        get
        {
            if (_lookAction == null) return Vector2.zero;
            Vector2 rawLook = _lookAction.ReadValue<Vector2>();

            if (_isGamepadActive)
            {
                return rawLook * _gamepadSensitivity * Time.deltaTime;
            }

            return rawLook * _mouseSensitivity;
        }
    }

    public bool IsFirePressed => _fireAction != null && _fireAction.IsPressed();
    public bool WasFireTriggered => _fireAction != null && _fireAction.WasPressedThisFrame();
    public bool IsAimPressed => _aimAction != null && _aimAction.IsPressed();
    public bool WasReloadTriggered => _reloadAction != null && _reloadAction.WasPressedThisFrame();
    public bool IsInteractPressed => _interactAction != null && _interactAction.IsPressed();
    public bool WasInteractTriggered => _interactAction != null && _interactAction.WasPressedThisFrame();
    public bool WasInteractReleased => _interactAction != null && _interactAction.WasReleasedThisFrame();
    public bool WasJumpTriggered => _jumpAction != null && _jumpAction.WasPressedThisFrame();
    public bool IsCrouchPressed => _crouchAction != null && _crouchAction.IsPressed();

    public bool IsWalkPressed
    {
        get
        {
            if (_walkAction != null && _walkAction.IsPressed()) return true;
            if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) return true;
            if (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed) return true;
            return false;
        }
    }

    public bool WasDropTriggered => _dropAction != null && _dropAction.WasPressedThisFrame();
    public bool WasSlot1Triggered => _slot1Action != null && _slot1Action.WasPressedThisFrame();
    public bool WasSlot2Triggered => _slot2Action != null && _slot2Action.WasPressedThisFrame();
    public bool WasSlot3Triggered => _slot3Action != null && _slot3Action.WasPressedThisFrame();
    public bool WasPauseTriggered => _pauseAction != null && _pauseAction.WasPressedThisFrame();
    public bool WasChatTriggered => _chatAction != null && _chatAction.WasPressedThisFrame();

    public bool IsScoreboardPressed
    {
        get
        {
            if (_scoreboardAction != null && _scoreboardAction.IsPressed()) return true;
            if (Keyboard.current != null && Keyboard.current.tabKey.isPressed) return true;
            if (Gamepad.current != null && Gamepad.current.selectButton.isPressed) return true;
            return false;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSensitivitySettings();
        InitializeInputActions();
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += HandleActionChange;
        EnableAllGameplayActions();
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= HandleActionChange;
        DisableAllGameplayActions();
    }

    private void OnDestroy()
    {
        _rebindOperation?.Dispose();
        if (_instance == this) _instance = null;
    }

    private void LoadSensitivitySettings()
    {
        _mouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, DEFAULT_MOUSE_SENSITIVITY);
        _gamepadSensitivity = PlayerPrefs.GetFloat(GAMEPAD_SENSITIVITY_KEY, DEFAULT_GAMEPAD_SENSITIVITY);
    }

    private void InitializeInputActions()
    {
        if (_inputActions == null)
        {
            BuildDefaultInputActionsAsset();
        }

        LoadBindingOverrides();
        CacheActionReferences();
        EnableAllGameplayActions();
    }

    private void BuildDefaultInputActionsAsset()
    {
        _inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap playerMap = _inputActions.AddActionMap(ACTION_MAP_PLAYER);

        InputAction move = playerMap.AddAction(ACTION_MOVE, InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        move.AddBinding("<Gamepad>/leftStick");

        InputAction look = playerMap.AddAction(ACTION_LOOK, InputActionType.Value);
        look.AddBinding("<Mouse>/delta");
        look.AddBinding("<Gamepad>/rightStick");

        InputAction fire = playerMap.AddAction(ACTION_FIRE, InputActionType.Button);
        fire.AddBinding("<Mouse>/leftButton");
        fire.AddBinding("<Gamepad>/rightTrigger");

        InputAction aim = playerMap.AddAction(ACTION_AIM, InputActionType.Button);
        aim.AddBinding("<Mouse>/rightButton");
        aim.AddBinding("<Gamepad>/leftTrigger");

        InputAction reload = playerMap.AddAction(ACTION_RELOAD, InputActionType.Button);
        reload.AddBinding("<Keyboard>/r");
        reload.AddBinding("<Gamepad>/buttonWest");

        InputAction interact = playerMap.AddAction(ACTION_INTERACT, InputActionType.Button);
        interact.AddBinding("<Keyboard>/e");
        interact.AddBinding("<Gamepad>/buttonNorth");

        InputAction jump = playerMap.AddAction(ACTION_JUMP, InputActionType.Button);
        jump.AddBinding("<Keyboard>/space");
        jump.AddBinding("<Gamepad>/buttonSouth");

        InputAction crouch = playerMap.AddAction(ACTION_CROUCH, InputActionType.Button);
        crouch.AddBinding("<Keyboard>/leftCtrl");
        crouch.AddBinding("<Gamepad>/buttonEast");

        InputAction walk = playerMap.AddAction(ACTION_WALK, InputActionType.Button);
        walk.AddBinding("<Keyboard>/leftShift");
        walk.AddBinding("<Gamepad>/leftStickPress");

        InputAction drop = playerMap.AddAction(ACTION_DROP, InputActionType.Button);
        drop.AddBinding("<Keyboard>/g");
        drop.AddBinding("<Gamepad>/dpad/down");

        InputAction slot1 = playerMap.AddAction(ACTION_SLOT_1, InputActionType.Button);
        slot1.AddBinding("<Keyboard>/1");
        slot1.AddBinding("<Gamepad>/dpad/left");

        InputAction slot2 = playerMap.AddAction(ACTION_SLOT_2, InputActionType.Button);
        slot2.AddBinding("<Keyboard>/2");
        slot2.AddBinding("<Gamepad>/dpad/up");

        InputAction slot3 = playerMap.AddAction(ACTION_SLOT_3, InputActionType.Button);
        slot3.AddBinding("<Keyboard>/3");
        slot3.AddBinding("<Gamepad>/dpad/right");

        InputAction pause = playerMap.AddAction(ACTION_PAUSE, InputActionType.Button);
        pause.AddBinding("<Keyboard>/escape");
        pause.AddBinding("<Gamepad>/start");

        InputAction scoreboard = playerMap.AddAction(ACTION_SCOREBOARD, InputActionType.Button);
        scoreboard.AddBinding("<Keyboard>/tab");
        scoreboard.AddBinding("<Gamepad>/select");

        InputAction chat = playerMap.AddAction(ACTION_CHAT, InputActionType.Button);
        chat.AddBinding("<Keyboard>/t");
    }

    private void CacheActionReferences()
    {
        if (_inputActions == null) return;

        InputActionMap map = _inputActions.FindActionMap(ACTION_MAP_PLAYER, false);
        if (map == null)
        {
            map = _inputActions.actionMaps.Count > 0 ? _inputActions.actionMaps[0] : _inputActions.AddActionMap(ACTION_MAP_PLAYER);
        }

        _moveAction = SafeGetAction(map, ACTION_MOVE);
        _lookAction = SafeGetAction(map, ACTION_LOOK);
        _fireAction = SafeGetAction(map, ACTION_FIRE, ACTION_ATTACK_ALT);
        _aimAction = SafeGetAction(map, ACTION_AIM);
        _reloadAction = SafeGetAction(map, ACTION_RELOAD);
        _interactAction = SafeGetAction(map, ACTION_INTERACT);
        _jumpAction = SafeGetAction(map, ACTION_JUMP);
        _crouchAction = SafeGetAction(map, ACTION_CROUCH);
        _walkAction = SafeGetAction(map, ACTION_WALK);
        _dropAction = SafeGetAction(map, ACTION_DROP);
        _slot1Action = SafeGetAction(map, ACTION_SLOT_1);
        _slot2Action = SafeGetAction(map, ACTION_SLOT_2);
        _slot3Action = SafeGetAction(map, ACTION_SLOT_3);
        _pauseAction = SafeGetAction(map, ACTION_PAUSE);
        _scoreboardAction = SafeGetAction(map, ACTION_SCOREBOARD);
        _chatAction = SafeGetAction(map, ACTION_CHAT);
    }

    private InputAction SafeGetAction(InputActionMap map, string primaryName, string secondaryName = null)
    {
        InputAction action = map.FindAction(primaryName, false);
        if (action != null) return action;

        if (!string.IsNullOrEmpty(secondaryName))
        {
            action = map.FindAction(secondaryName, false);
            if (action != null) return action;
        }

        action = map.AddAction(primaryName, InputActionType.Button);

        if (primaryName == ACTION_WALK)
        {
            action.AddBinding("<Keyboard>/leftShift");
            action.AddBinding("<Gamepad>/leftStickPress");
        }
        else if (primaryName == ACTION_SCOREBOARD)
        {
            action.AddBinding("<Keyboard>/tab");
            action.AddBinding("<Gamepad>/select");
        }
        else if (primaryName == ACTION_CHAT)
        {
            action.AddBinding("<Keyboard>/t");
        }
        else if (primaryName == ACTION_SLOT_1)
        {
            action.AddBinding("<Keyboard>/1");
            action.AddBinding("<Gamepad>/dpad/left");
        }
        else if (primaryName == ACTION_SLOT_2)
        {
            action.AddBinding("<Keyboard>/2");
            action.AddBinding("<Gamepad>/dpad/up");
        }
        else if (primaryName == ACTION_SLOT_3)
        {
            action.AddBinding("<Keyboard>/3");
            action.AddBinding("<Gamepad>/dpad/right");
        }
        else if (primaryName == ACTION_DROP)
        {
            action.AddBinding("<Keyboard>/g");
            action.AddBinding("<Gamepad>/dpad/down");
        }
        else if (primaryName == ACTION_RELOAD)
        {
            action.AddBinding("<Keyboard>/r");
            action.AddBinding("<Gamepad>/buttonWest");
        }
        else if (primaryName == ACTION_INTERACT)
        {
            action.AddBinding("<Keyboard>/e");
            action.AddBinding("<Gamepad>/buttonNorth");
        }

        return action;
    }

    private void EnableAllGameplayActions()
    {
        if (_inputActions != null)
        {
            _inputActions.Enable();
        }
    }

    private void DisableAllGameplayActions()
    {
        if (_inputActions != null)
        {
            _inputActions.Disable();
        }
    }

    private void HandleActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;

        InputAction action = obj as InputAction;
        if (action?.activeControl == null) return;

        InputDevice device = action.activeControl.device;
        bool isCurrentGamepad = device is Gamepad;

        if (isCurrentGamepad != _isGamepadActive)
        {
            _isGamepadActive = isCurrentGamepad;
            OnInputDeviceChanged?.Invoke(_isGamepadActive);
        }
    }

    public InputAction GetAction(string actionName)
    {
        return _inputActions?.FindAction(actionName, false);
    }

    public string GetBindingDisplayString(string actionName, int bindingIndex)
    {
        InputAction action = GetAction(actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return string.Empty;
        }

        return action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
    }

    public int GetBindingIndex(string actionName, bool targetGamepad, string compositePart = null)
    {
        InputAction action = GetAction(actionName);
        if (action == null) return -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (!string.IsNullOrEmpty(compositePart))
            {
                if (binding.isPartOfComposite && string.Equals(binding.name, compositePart, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
                continue;
            }

            if (binding.isComposite) continue;

            bool isGamepadBinding = binding.path.Contains("Gamepad");
            if (targetGamepad == isGamepadBinding)
            {
                return i;
            }
        }

        return -1;
    }

    public void StartInteractiveRebind(string actionName, int bindingIndex, Action<string> onComplete, Action onCancel)
    {
        InputAction action = GetAction(actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            onCancel?.Invoke();
            return;
        }

        action.Disable();
        _rebindOperation?.Cancel();
        _rebindOperation?.Dispose();

        bool isTargetingGamepad = action.bindings[bindingIndex].path.Contains("Gamepad");

        _rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .WithCancelingThrough("<Gamepad>/start")
            .OnMatchWaitForAnother(0.1f)
            .OnCancel(operation =>
            {
                action.Enable();
                _rebindOperation?.Dispose();
                _rebindOperation = null;
                onCancel?.Invoke();
            })
            .OnComplete(operation =>
            {
                action.Enable();
                string displayString = action.GetBindingDisplayString(bindingIndex);
                SaveBindingOverrides();
                _rebindOperation?.Dispose();
                _rebindOperation = null;
                OnBindingsRebound?.Invoke();
                onComplete?.Invoke(displayString);
            });

        if (isTargetingGamepad)
        {
            _rebindOperation.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else
        {
            _rebindOperation.WithControlsExcluding("<Gamepad>");
        }

        _rebindOperation.Start();
    }

    public void ResetAllBindingsToDefault()
    {
        if (_inputActions == null) return;

        _inputActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BINDINGS_PREFS_KEY);
        PlayerPrefs.Save();
        OnBindingsRebound?.Invoke();
    }

    private void SaveBindingOverrides()
    {
        if (_inputActions == null) return;

        string overridesJson = _inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BINDINGS_PREFS_KEY, overridesJson);
        PlayerPrefs.Save();
    }

    private void LoadBindingOverrides()
    {
        if (_inputActions == null) return;

        string overridesJson = PlayerPrefs.GetString(BINDINGS_PREFS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(overridesJson))
        {
            _inputActions.LoadBindingOverridesFromJson(overridesJson);
        }
    }

    public void SetGameplayInputActive(bool active)
    {
        if (_inputActions == null) return;

        if (active)
        {
            EnableAllGameplayActions();
        }
        else
        {
            DisableAllGameplayActions();
        }
    }

}