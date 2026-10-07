using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ColorBlindSettingsUI : MonoBehaviour
{
    public TMP_Dropdown modeDropdown;
    public Slider strengthSlider;
    public Toggle correctToggle;

    void OnEnable()
    {
        var cb = ColorBlindManager.Instance;
        if (cb == null) return;

        if (modeDropdown)
        {
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Normal", "Protanopia", "Deuteranopia",
                "Tritanopia", "Acromatopsia"
            });
            modeDropdown.SetValueWithoutNotify((int)cb.Mode);
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
        }
        if (strengthSlider)
        {
            strengthSlider.minValue = 0; strengthSlider.maxValue = 1;
            strengthSlider.SetValueWithoutNotify(cb.Strength);
            strengthSlider.onValueChanged.AddListener(OnStrengthChanged);
        }
        if (correctToggle)
        {
            correctToggle.SetIsOnWithoutNotify(cb.Correct);
            correctToggle.onValueChanged.AddListener(OnCorrectChanged);
        }
    }

    void OnDisable()
    {

        if (modeDropdown) modeDropdown.onValueChanged.RemoveListener(OnModeChanged);
        if (strengthSlider) strengthSlider.onValueChanged.RemoveListener(OnStrengthChanged);
        if (correctToggle) correctToggle.onValueChanged.RemoveListener(OnCorrectChanged);
    }

    void OnModeChanged(int v)       => ColorBlindManager.Instance.SetMode((ColorBlindManager.FilterMode)v);
    void OnStrengthChanged(float v) => ColorBlindManager.Instance.SetStrength(v);
    void OnCorrectChanged(bool v)   => ColorBlindManager.Instance.SetCorrect(v);
}
