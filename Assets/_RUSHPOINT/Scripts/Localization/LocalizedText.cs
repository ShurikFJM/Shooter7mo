using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    private TMP_Text textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        LocalizationManager.Source.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDestroy()
    {
        LocalizationManager.Source.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText()
    {
        textComponent.text = LocalizationManager.Source.GetLocalizedText(localizationKey);
    }

    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }
}
