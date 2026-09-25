using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI en world-space sobre una DominationZone: nombre de la zona, estado
/// (neutral / capturando / disputada / dominada) y barra de progreso coloreada
/// según el equipo. Colocar como hijo del GameObject de la zona, dentro de un
/// Canvas en modo "World Space" (Render Mode = World Space, escala pequeña,
/// ej. 0.01 en los 3 ejes, y bien arriba de la zona en el eje Y).
/// </summary>
public class DominationZoneUI : MonoBehaviour
{
    [SerializeField] private DominationZone zone;

    [Header("Referencias UI")]
    [SerializeField] private TMP_Text zoneNameText;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Image con Type = Filled, Fill Method = Horizontal")]
    [SerializeField] private Image progressFill;

    [Header("Colores")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color redColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color blueColor = new Color(0.15f, 0.4f, 0.85f);

    [Header("Textos")]
    [SerializeField] private string contestedLabel = "¡Disputada!";
    [SerializeField] private string capturingLabel = "Capturando...";
    [SerializeField] private string ownedLabel = "Dominada: {0}";
    [SerializeField] private string neutralLabel = "Neutral";
    [SerializeField] private string redName = "Rojo";
    [SerializeField] private string blueName = "Azul";

    private Camera mainCamera;

    private void Start()
    {
        if (zoneNameText != null && zone != null) zoneNameText.text = zone.ZoneId;
    }

    private void LateUpdate()
    {
        if (zone == null) return;

        FaceCamera();
        RefreshUI();
    }

    private void FaceCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        transform.forward = transform.position - mainCamera.transform.position;
    }

    private void RefreshUI()
    {
        float value = zone.CaptureValue.Value; // -1..1
        float absValue = Mathf.Abs(value);

        if (progressFill != null)
        {
            progressFill.fillAmount = absValue;
            progressFill.color = value >= 0f
                ? Color.Lerp(neutralColor, redColor, absValue)
                : Color.Lerp(neutralColor, blueColor, absValue);
        }

        if (statusText == null) return;

        if (zone.IsContested)
        {
            statusText.text = contestedLabel;
        }
        else if (zone.OwnerTeam == Team.Neutral)
        {
            statusText.text = absValue > 0.01f ? capturingLabel : neutralLabel;
        }
        else
        {
            statusText.text = string.Format(ownedLabel, zone.OwnerTeam == Team.Red ? redName : blueName);
        }
    }
}
