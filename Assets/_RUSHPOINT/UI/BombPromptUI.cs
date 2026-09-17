using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI básica de feedback para la interacción con la bomba. No es un NetworkBehaviour:
/// se coloca en un Canvas normal de la escena y busca automáticamente el
/// BombInteractor del jugador local para mostrar el prompt y el progreso de plantado.
/// Usa TextMeshPro para el texto (Image sigue siendo UI clásica, ya que TMP no
/// maneja fill bars).
/// </summary>
public class BombPromptUI : MonoBehaviour
{
    [Header("Prompt de acción")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    [Header("Barra de progreso (plantado)")]
    [Tooltip("Image con Type = Filled (Horizontal o Radial 360)")]
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressLabel;

    [Header("Textos")]
    [SerializeField] private string pickupPrompt = "Presiona [E] para recoger la bomba";
    [SerializeField] private string dropPrompt = "Presiona [E] para soltar la bomba";
    [SerializeField] private string plantPrompt = "Mantén [E] para plantar la bomba";
    [SerializeField] private string plantingLabel = "Plantando...";

    private BombInteractor localInteractor;

    private void Update()
    {
        if (localInteractor == null)
        {
            TryFindLocalInteractor();
            if (localInteractor == null)
            {
                HideAll();
                return;
            }
        }

        RefreshUI();
    }

    private void TryFindLocalInteractor()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        NetworkObject localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject == null) return;

        localInteractor = localPlayerObject.GetComponent<BombInteractor>();
    }

    private void RefreshUI()
    {
        if (localInteractor.IsPlanting)
        {
            HidePrompt();
            ShowProgress();
            return;
        }

        HideProgress();

        if (localInteractor.IsCarryingBomb)
        {
            ShowPrompt(localInteractor.IsInSite ? plantPrompt : dropPrompt);
        }
        else if (localInteractor.HasNearbyBomb)
        {
            ShowPrompt(pickupPrompt);
        }
        else
        {
            HidePrompt();
        }
    }

    private void ShowPrompt(string message)
    {
        if (promptRoot != null) promptRoot.SetActive(true);
        if (promptText != null) promptText.text = message;
    }

    private void HidePrompt()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }

    private void ShowProgress()
    {
        if (progressRoot != null) progressRoot.SetActive(true);
        if (progressFill != null) progressFill.fillAmount = localInteractor.PlantProgress01;
        if (progressLabel != null) progressLabel.text = plantingLabel;
    }

    private void HideProgress()
    {
        if (progressRoot != null) progressRoot.SetActive(false);
    }

    private void HideAll()
    {
        HidePrompt();
        HideProgress();
    }
}
