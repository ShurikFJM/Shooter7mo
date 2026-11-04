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

    [SerializeField] private string _pickupPrompt = "Press [{0}] to pick up bomb";
    [SerializeField] private string _dropPrompt = "Press [{0}] to drop bomb";
    [SerializeField] private string _plantPrompt = "Hold [{0}] to plant bomb";
    [SerializeField] private string _defusePrompt = "Hold [{0}] to defuse bomb";
    [SerializeField] private string _plantingLabel = "Planting...";
    [SerializeField] private string _defusingLabel = "Defusing...";

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

    private string GetActionKeyName(string actionName, string fallbackKey)
    {
        if (InputManager.Instance == null) return fallbackKey;

        bool isGamepad = InputManager.Instance.IsGamepadActive;
        int bindingIndex = InputManager.Instance.GetBindingIndex(actionName, isGamepad);

        if (bindingIndex >= 0)
        {
            string display = InputManager.Instance.GetBindingDisplayString(actionName, bindingIndex);
            if (!string.IsNullOrEmpty(display)) return display;
        }

        if (isGamepad)
        {
            if (actionName == "Interact") return "Y";
            if (actionName == "Drop") return "D-Pad Down";
        }

        return fallbackKey;
    }

    private string FormatPrompt(string template, string keyName)
    {
        if (template.Contains("{0}")) return string.Format(template, keyName);
        if (template.Contains("[E]")) return template.Replace("[E]", $"[{keyName}]");
        if (template.Contains("[G]")) return template.Replace("[G]", $"[{keyName}]");
        return $"{template} [{keyName}]";
    }

    private void RefreshUserInterface()
    {
        if (_localInteractor.IsPlanting)
        {
            HideActionPrompt();
            ShowProgressDisplay(_plantingLabel, _localInteractor.PlantProgressNormalized);
            return;
        }

        if (_localInteractor.IsDefusing)
        {
            HideActionPrompt();
            ShowProgressDisplay(_defusingLabel, _localInteractor.DefuseProgressNormalized);
            return;
        }

        HideProgressDisplay();

        string interactKey = GetActionKeyName("Interact", "E");
        string dropKey = GetActionKeyName("Drop", "G");

        if (_localInteractor.IsCarryingBomb)
        {
            string prompt = _localInteractor.IsInSite
                ? FormatPrompt(_plantPrompt, interactKey)
                : FormatPrompt(_dropPrompt, dropKey);
            ShowActionPrompt(prompt);
        }
        else if (_localInteractor.HasNearbyPlantedBomb)
        {
            ShowActionPrompt(FormatPrompt(_defusePrompt, interactKey));
        }
        else if (_localInteractor.HasNearbyBomb)
        {
            ShowActionPrompt(FormatPrompt(_pickupPrompt, interactKey));
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

    private void ShowProgressDisplay(string label, float progressNormalized)
    {
        if (_progressRoot != null) _progressRoot.SetActive(true);
        if (_progressFill != null) _progressFill.fillAmount = progressNormalized;
        if (_progressLabel != null) _progressLabel.text = label;
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