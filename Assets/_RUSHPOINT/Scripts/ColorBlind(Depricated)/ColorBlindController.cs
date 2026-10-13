using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el filtro de daltonismo y la UI.
/// - URP: asigna el mismo Material que usa el Fullscreen Pass Renderer Feature.
/// - Built-in: pon este script en la cámara y asigna un Material con ColorBlind_BuiltIn.
/// </summary>
[DisallowMultipleComponent]
public class ColorBlindController : MonoBehaviour
{
    public enum FilterMode { Normal = 0, Protanopia = 1, Deuteranopia = 2, Tritanopia = 3, Acromatopsia = 4 }

    [Header("Material")]
    [Tooltip("Material con el shader ColorBlind_URP o ColorBlind_BuiltIn")]
    public Material material;

    [Header("UI (opcional)")]
    public TMP_Dropdown modeDropdown;
    public Slider strengthSlider;
    public Toggle correctToggle;   // ON = corregir (daltonizar), OFF = simular

    [Header("Estado")]
    public FilterMode mode = FilterMode.Normal;
    [Range(0, 1)] public float strength = 1f;
    public bool correct = false;

    const string KeyMode = "cb_mode", KeyStrength = "cb_strength", KeyCorrect = "cb_correct";
    static readonly int ModeId = Shader.PropertyToID("_Mode");
    static readonly int StrengthId = Shader.PropertyToID("_Strength");
    static readonly int CorrectId = Shader.PropertyToID("_Correct");

    void Start()
    {
        // Cargar preferencias guardadas
        mode = (FilterMode)PlayerPrefs.GetInt(KeyMode, (int)mode);
        strength = PlayerPrefs.GetFloat(KeyStrength, strength);
        correct = PlayerPrefs.GetInt(KeyCorrect, correct ? 1 : 0) == 1;

        if (modeDropdown)
        {
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Normal", "Protanopia (rojo)", "Deuteranopia (verde)",
                "Tritanopia (azul)", "Acromatopsia (grises)"
            });
            modeDropdown.SetValueWithoutNotify((int)mode);
            modeDropdown.onValueChanged.AddListener(v => SetMode(v));
        }
        if (strengthSlider)
        {
            strengthSlider.minValue = 0; strengthSlider.maxValue = 1;
            strengthSlider.SetValueWithoutNotify(strength);
            strengthSlider.onValueChanged.AddListener(SetStrength);
        }
        if (correctToggle)
        {
            correctToggle.SetIsOnWithoutNotify(correct);
            correctToggle.onValueChanged.AddListener(SetCorrect);
        }
        Apply();
    }

    public void SetMode(int m) { mode = (FilterMode)m; Apply(); }
    public void SetStrength(float s) { strength = s; Apply(); }
    public void SetCorrect(bool c) { correct = c; Apply(); }

    void Apply()
    {
        if (!material) return;
        material.SetFloat(ModeId, (float)mode);
        material.SetFloat(StrengthId, strength);
        material.SetFloat(CorrectId, correct ? 1f : 0f);

        PlayerPrefs.SetInt(KeyMode, (int)mode);
        PlayerPrefs.SetFloat(KeyStrength, strength);
        PlayerPrefs.SetInt(KeyCorrect, correct ? 1 : 0);
    }

    void OnValidate() { Apply(); }

    // Solo se ejecuta en pipeline Built-in (en URP se ignora)
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (material) Graphics.Blit(src, dst, material);
        else Graphics.Blit(src, dst);
    }

    // Deja el material en estado neutro al salir (evita modificar el asset en el editor)
    void OnDisable()
    {
        if (material) material.SetFloat(ModeId, 0f);
    }
}
