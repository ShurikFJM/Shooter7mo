using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BombPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFill;
    [SerializeField] private TMP_Text _progressLabel;

    [SerializeField] private string _pickupPrompt = "Press [E] to pick up bomb";
    [SerializeField] private string _dropPrompt = "Press [E] to drop bomb";
    [SerializeField] private string _plantPrompt = "Hold [E] to plant bomb";
    [SerializeField] private string _plantingLabel = "Planting...";

    private BombInteractor _localInteractor;

    private void Update()
    {
        if (_localInteractor == null)
        {
            FindLocalPlayerInteractor();
            if (_localInteractor == null)
            {
                HideAllUIElements();
                return;
            }
        }

        RefreshUserInterface();
    }

    private void FindLocalPlayerInteractor()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        NetworkObject localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject == null) return;

        _localInteractor = localPlayerObject.GetComponent<BombInteractor>();
    }

    private void RefreshUserInterface()
    {
        if (_localInteractor.IsPlanting)
        {
            HideActionPrompt();
            ShowProgressDisplay();
            return;
        }

        HideProgressDisplay();

        if (_localInteractor.IsCarryingBomb)
        {
            ShowActionPrompt(_localInteractor.IsInSite ? _plantPrompt : _dropPrompt);
        }
        else if (_localInteractor.HasNearbyBomb)
        {
            ShowActionPrompt(_pickupPrompt);
        }
        else
        {
            HideActionPrompt();
        }
    }

    private void ShowActionPrompt(string messageContent)
    {
        if (_promptRoot != null) _promptRoot.SetActive(true);
        if (_promptText != null) _promptText.text = messageContent;
    }

    private void HideActionPrompt()
    {
        if (_promptRoot != null) _promptRoot.SetActive(false);
    }

    private void ShowProgressDisplay()
    {
        if (_progressRoot != null) _progressRoot.SetActive(true);
        if (_progressFill != null) _progressFill.fillAmount = _localInteractor.PlantProgressNormalized;
        if (_progressLabel != null) _progressLabel.text = _plantingLabel;
    }

    private void HideProgressDisplay()
    {
        if (_progressRoot != null) _progressRoot.SetActive(false);
    }

    private void HideAllUIElements()
    {
        HideActionPrompt();
        HideProgressDisplay();
    }
}