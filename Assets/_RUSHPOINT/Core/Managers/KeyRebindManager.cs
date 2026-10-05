using UnityEngine;
using UnityEngine.InputSystem;

public class KeyRebindManager : MonoBehaviour
{
    private const string _BINDINGS_PREFS_KEY = "CUSTOM_PLAYER_INPUT_BINDINGS";

    public static KeyRebindManager Instance { get; private set; }

    [SerializeField] private InputActionAsset _inputActionAsset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCustomBindings();
    }

    public void SaveCustomBindings()
    {
        if (_inputActionAsset == null) return;

        string bindingsJson = _inputActionAsset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(_BINDINGS_PREFS_KEY, bindingsJson);
        PlayerPrefs.Save();
    }

    public void LoadCustomBindings()
    {
        if (_inputActionAsset == null) return;

        if (PlayerPrefs.HasKey(_BINDINGS_PREFS_KEY))
        {
            string bindingsJson = PlayerPrefs.GetString(_BINDINGS_PREFS_KEY);
            _inputActionAsset.LoadBindingOverridesFromJson(bindingsJson);
        }
    }

    public void ResetAllBindingsToDefault()
    {
        if (_inputActionAsset == null) return;

        _inputActionAsset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(_BINDINGS_PREFS_KEY);
        PlayerPrefs.Save();

        KeyRebindButtonUI[] uiButtons = FindObjectsByType<KeyRebindButtonUI>(FindObjectsInactive.Exclude);
        foreach (KeyRebindButtonUI buttonUI in uiButtons)
        {
            buttonUI.UpdateBindingDisplay();
        }
    }
}