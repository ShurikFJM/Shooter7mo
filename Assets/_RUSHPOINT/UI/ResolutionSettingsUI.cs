using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResolutionSettingsUI : MonoBehaviour
{
    private const string PREF_WIDTH_KEY = "Rushpoint_ResWidth";
    private const string PREF_HEIGHT_KEY = "Rushpoint_ResHeight";

    [SerializeField] private TMP_Dropdown _resolutionDropdown;

    private readonly List<Resolution> _uniqueResolutions = new List<Resolution>();
    private bool _isInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedResolutionOnStartup()
    {
        if (!PlayerPrefs.HasKey(PREF_WIDTH_KEY) || !PlayerPrefs.HasKey(PREF_HEIGHT_KEY)) return;

        int savedWidth = PlayerPrefs.GetInt(PREF_WIDTH_KEY);
        int savedHeight = PlayerPrefs.GetInt(PREF_HEIGHT_KEY);

        if (savedWidth >= 640 && savedHeight >= 480)
        {
            Screen.SetResolution(savedWidth, savedHeight, Screen.fullScreenMode);
        }
    }

    private void Awake()
    {
        if (_resolutionDropdown == null)
        {
            _resolutionDropdown = GetComponent<TMP_Dropdown>();
        }
    }

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            PopulateResolutions();
            _isInitialized = true;
        }

        SyncDropdownToCurrentResolution();

        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }
    }

    private void OnDisable()
    {
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
        }
    }

    private void PopulateResolutions()
    {
        if (_resolutionDropdown == null) return;

        _uniqueResolutions.Clear();
        Resolution[] allResolutions = Screen.resolutions;
        List<string> options = new List<string>(allResolutions.Length);

        for (int i = allResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = allResolutions[i];
            if (res.width < 800 || res.height < 600) continue;

            bool alreadyAdded = false;
            for (int j = 0; j < _uniqueResolutions.Count; j++)
            {
                if (_uniqueResolutions[j].width == res.width && _uniqueResolutions[j].height == res.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                _uniqueResolutions.Add(res);
                options.Add($"{res.width} X {res.height}");
            }
        }

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(options);
    }

    private void SyncDropdownToCurrentResolution()
    {
        if (_resolutionDropdown == null || _uniqueResolutions.Count == 0) return;

        int targetWidth = PlayerPrefs.GetInt(PREF_WIDTH_KEY, Screen.width);
        int targetHeight = PlayerPrefs.GetInt(PREF_HEIGHT_KEY, Screen.height);
        int matchedIndex = 0;

        for (int i = 0; i < _uniqueResolutions.Count; i++)
        {
            if (_uniqueResolutions[i].width == targetWidth && _uniqueResolutions[i].height == targetHeight)
            {
                matchedIndex = i;
                break;
            }
        }

        _resolutionDropdown.SetValueWithoutNotify(matchedIndex);
        _resolutionDropdown.RefreshShownValue();
    }

    private void HandleResolutionChanged(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _uniqueResolutions.Count) return;

        Resolution chosen = _uniqueResolutions[optionIndex];
        Screen.SetResolution(chosen.width, chosen.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt(PREF_WIDTH_KEY, chosen.width);
        PlayerPrefs.SetInt(PREF_HEIGHT_KEY, chosen.height);
        PlayerPrefs.Save();
    }
}