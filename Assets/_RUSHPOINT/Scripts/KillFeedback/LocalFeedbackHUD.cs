using UnityEngine;
using TMPro;

public class LocalFeedbackHUD : MonoBehaviour
{
    public static LocalFeedbackHUD Instance { get; private set; }

    [Header("Referencias de UI en la Escena")]
    public CanvasGroup DamageCanvasGroup;
    public GameObject DeathPanel;
    public TMP_Text DeathText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}