using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum TutorialState
{
    Welcome,
    PingSystem,
    BombPlanting,
    RoleSwapping,
    MedicTraining,
    Completed
}

public class TutorialManager : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _instructionText;

    private TutorialState _currentState = TutorialState.Welcome;
    private bool _hasNormalPing;
    private bool _hasDangerPing;
    private bool _hasGroupPing;

    private void Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartHost();
        }

        UpdateInstructionUI();
    }

    public void AdvanceToPingState()
    {
        if (_currentState != TutorialState.Welcome) return;

        _currentState = TutorialState.PingSystem;
        UpdateInstructionUI();
    }

    public void OnPingPlaced(int pingType)
    {
        if (_currentState != TutorialState.PingSystem) return;

        if (pingType == 0) _hasNormalPing = true;
        if (pingType == 1) _hasDangerPing = true;
        if (pingType == 2) _hasGroupPing = true;

        if (_hasNormalPing && _hasDangerPing && _hasGroupPing)
        {
            _currentState = TutorialState.BombPlanting;
            UpdateInstructionUI();
        }
    }

    public void OnBombPlanted()
    {
        if (_currentState != TutorialState.BombPlanting) return;

        if (_instructionText != null)
        {
            _instructionText.text = "Bomb planted. Proceed to defuse it.";
        }
    }

    public void OnBombDefused()
    {
        if (_currentState != TutorialState.BombPlanting) return;

        _currentState = TutorialState.RoleSwapping;
        UpdateInstructionUI();
    }

    public void OnRoleSwappedTo(string roleName)
    {
        if (_currentState != TutorialState.RoleSwapping) return;

        if (roleName == "Medic")
        {
            _currentState = TutorialState.MedicTraining;
            UpdateInstructionUI();
        }
    }

    public void CheckMedicTrainingProgress(float currentTargetHealth)
    {
        if (_currentState != TutorialState.MedicTraining) return;

        if (currentTargetHealth >= 100f)
        {
            _currentState = TutorialState.Completed;
            UpdateInstructionUI();
        }
    }

    private void UpdateInstructionUI()
    {
        if (_instructionText == null) return;

        switch (_currentState)
        {
            case TutorialState.Welcome:
                _instructionText.text = "Welcome to the tutorial. Move forward to begin.";
                break;
            case TutorialState.PingSystem:
                _instructionText.text = "Place Normal, Danger, and Group pings.";
                break;
            case TutorialState.BombPlanting:
                _instructionText.text = "Pick up the C4 Bomb, plant it in the designated zone, and then defuse it.";
                break;
            case TutorialState.RoleSwapping:
                _instructionText.text = "Access the terminal and swap your role to Medic.";
                break;
            case TutorialState.MedicTraining:
                _instructionText.text = "Use your Healing Pistol to restore the target NPC health to 100%.";
                break;
            case TutorialState.Completed:
                _instructionText.text = "Tutorial complete. Returning to main menu.";
                break;
        }
    }
}