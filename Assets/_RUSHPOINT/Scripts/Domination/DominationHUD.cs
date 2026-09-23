using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Prompt de pantalla (mismo Canvas que BombPromptUI) que le dice al jugador
/// local si está capturando, defendiendo, o disputando una zona de dominación.
/// No es un NetworkBehaviour: busca automáticamente al jugador local.
/// </summary>
public class DominationHUD : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    [Header("Textos ({0} = nombre de la zona)")]
    [SerializeField] private string capturingLabel = "Capturando {0}...";
    [SerializeField] private string defendingLabel = "Defendiendo {0}";
    [SerializeField] private string contestedLabel = "¡{0} disputada!";

    private PlayerZoneDetector localDetector;
    private PlayerTeam localTeam;

    private void Update()
    {
        if (localDetector == null || localTeam == null)
        {
            TryFindLocal();
            if (localDetector == null || localTeam == null)
            {
                Hide();
                return;
            }
        }

        Refresh();
    }

    private void TryFindLocal()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        NetworkObject localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject == null) return;

        localDetector = localPlayerObject.GetComponent<PlayerZoneDetector>();
        localTeam = localPlayerObject.GetComponent<PlayerTeam>();
    }

    private void Refresh()
    {
        DominationZone zone = localDetector.CurrentZone;
        if (zone == null)
        {
            Hide();
            return;
        }

        if (promptRoot != null) promptRoot.SetActive(true);
        if (promptText == null) return;

        if (zone.IsContested)
        {
            promptText.text = string.Format(contestedLabel, zone.ZoneId);
        }
        else if (zone.OwnerTeam == localTeam.CurrentTeam.Value)
        {
            promptText.text = string.Format(defendingLabel, zone.ZoneId);
        }
        else
        {
            promptText.text = string.Format(capturingLabel, zone.ZoneId);
        }
    }

    private void Hide()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }
}
