using UnityEngine;

/// <summary>
/// Manager persistente del filtro de daltonismo. Se crea solo al iniciar el juego
/// y sobrevive a los cambios de escena. Requiere un Material en:
///   Assets/Resources/ColorBlindMaterial.mat   (shader ColorBlind_URP o ColorBlind_BuiltIn)
/// </summary>
public class ColorBlindManager : MonoBehaviour
{
    public enum FilterMode { Normal = 0, Protanopia = 1, Deuteranopia = 2, Tritanopia = 3, Acromatopsia = 4 }

    public static ColorBlindManager Instance { get; private set; }

    public Material Material { get; private set; }
    public FilterMode Mode { get; private set; }
    public float Strength { get; private set; } = 1f;
    public bool Correct { get; private set; }

    const string KeyMode = "cb_mode", KeyStrength = "cb_strength", KeyCorrect = "cb_correct";
    static readonly int ModeId = Shader.PropertyToID("_Mode");
    static readonly int StrengthId = Shader.PropertyToID("_Strength");
    static readonly int CorrectId = Shader.PropertyToID("_Correct");

    // Se ejecuta automáticamente al arrancar el juego, sin poner nada en la escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("ColorBlindManager");
        go.AddComponent<ColorBlindManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Material = Resources.Load<Material>("ColorBlindMaterial");
        if (Material == null)
            Debug.LogError("[ColorBlind] Falta Assets/Resources/ColorBlindMaterial.mat");

        Mode = (FilterMode)PlayerPrefs.GetInt(KeyMode, 0);
        Strength = PlayerPrefs.GetFloat(KeyStrength, 1f);
        Correct = PlayerPrefs.GetInt(KeyCorrect, 0) == 1;
        Apply();
    }

    public void SetMode(FilterMode m) { Mode = m; Apply(); }
    public void SetStrength(float s)  { Strength = Mathf.Clamp01(s); Apply(); }
    public void SetCorrect(bool c)    { Correct = c; Apply(); }

    void Apply()
    {
        if (Material != null)
        {
            Material.SetFloat(ModeId, (float)Mode);
            Material.SetFloat(StrengthId, Strength);
            Material.SetFloat(CorrectId, Correct ? 1f : 0f);
        }
        PlayerPrefs.SetInt(KeyMode, (int)Mode);
        PlayerPrefs.SetFloat(KeyStrength, Strength);
        PlayerPrefs.SetInt(KeyCorrect, Correct ? 1 : 0);
    }

    // Solo al cerrar el juego: deja el asset neutro para que no quede modificado en el editor
    void OnApplicationQuit()
    {
        if (Material != null) Material.SetFloat(ModeId, 0f);
    }
}
