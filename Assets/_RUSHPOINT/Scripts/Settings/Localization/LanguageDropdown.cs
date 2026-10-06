using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LanguageDropdown : MonoBehaviour
{
    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();

        InitializeDropdown();
        _dropdown.onValueChanged.AddListener(OnLanguageSelected);
    }

    private void Start()
    {
        LocalizationManager.Source.OnLanguageChanged += UpdateDropdownLabels;
        UpdateDropdownLabels();
    }

    private void OnDestroy()
    {
        LocalizationManager.Source.OnLanguageChanged -= UpdateDropdownLabels;
        _dropdown.onValueChanged.RemoveListener(OnLanguageSelected);
    }

    private void InitializeDropdown()
    {
        List<string> options = new List<string>();

        foreach (string key in LocalizationExtensions.LanguageKeys)
        {
            options.Add(key.Localize());
        }

        _dropdown.ClearOptions();
        _dropdown.AddOptions(options);

        _dropdown.value = GetCurrentLanguageIndex();
        _dropdown.RefreshShownValue();
    }

    private void OnLanguageSelected(int index)
    {
        LocalizationManager.Source.SetLanguage(LocalizationExtensions.LanguageKeys[index]);
    }

    private void UpdateDropdownLabels()
    {
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var key in LocalizationExtensions.LanguageKeys)
        {
            options.Add(new TMP_Dropdown.OptionData(key.Localize()));
        }

        _dropdown.options = options;
        _dropdown.SetValueWithoutNotify(GetCurrentLanguageIndex());
        _dropdown.RefreshShownValue();
    }

    private int GetCurrentLanguageIndex()
    {
        string current = LocalizationManager.Source.CurrentLanguage;

        for (int i = 0; i < LocalizationExtensions.LanguageKeys.Length; i++)
        {
            if (LocalizationExtensions.LanguageKeys[i] == current)
            {
                return i;
            }
        }
        return 0;
    }
}
