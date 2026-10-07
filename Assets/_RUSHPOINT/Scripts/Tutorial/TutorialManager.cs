using UnityEngine;
using Unity.Netcode;
using TMPro;

public enum TutorialState
{
    Welcome,
    PickUpBomb,
    MoveToSideA,
    BombPlanting,
    BombDefusing,
    PingSystem,
    RoleSwapping,
    MedicTraining,
    Completed
}

public class TutorialManager : NetworkBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _instructionText;
    [SerializeField] private Transform _terroristSpawnPoint;
    [SerializeField] private GameObject _roleSelecterPanel;
    [SerializeField] private GameObject _tacticalHudCanvas;
    [SerializeField] private GameObject _minimapCanvas;

    private TutorialState _currentState;
    private Bomb _sceneBomb;
    private bool _hasPingNormal;
    private bool _hasPingDanger;
    private bool _hasPingGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeUiState();

        if (NetworkManager.Singleton != null)
        {
            if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.StartHost();
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                PositionAndAssignTeam();
            }
        }

        SetState(TutorialState.Welcome);
        Invoke(nameof(InitializeTutorialBombListener), 1.5f);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }

        if (_sceneBomb != null)
        {
            _sceneBomb.State.OnValueChanged -= HandleBombStateChanged;
        }
    }

    private void InitializeUiState()
    {
        RoleSelectScreenUI roleScreen = FindAnyObjectByType<RoleSelectScreenUI>();
        if (roleScreen != null)
        {
            roleScreen.CloseRoleScreen();
        }

        if (_roleSelecterPanel != null)
        {
            _roleSelecterPanel.SetActive(false);
        }

        if (_tacticalHudCanvas != null)
        {
            _tacticalHudCanvas.SetActive(true);
        }

        if (_minimapCanvas != null)
        {
            _minimapCanvas.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleServerStarted()
    {
        Invoke(nameof(PositionAndAssignTeam), 0.2f);
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Invoke(nameof(PositionAndAssignTeam), 0.2f);
        }
    }

    private void PositionAndAssignTeam()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
        {
            PlayerTeam playerTeam = localPlayer.GetComponent<PlayerTeam>();
            if (playerTeam != null)
            {
                playerTeam.CurrentTeam.Value = Team.Red;
            }

            Canvas playerCanvas = localPlayer.GetComponentInChildren<Canvas>(true);
            if (playerCanvas != null)
            {
                playerCanvas.gameObject.SetActive(true);
            }

            if (_terroristSpawnPoint != null)
            {
                CharacterController characterController = localPlayer.GetComponent<CharacterController>();
                if (characterController != null) characterController.enabled = false;

                localPlayer.transform.position = _terroristSpawnPoint.position;
                localPlayer.transform.rotation = _terroristSpawnPoint.rotation;

                if (characterController != null) characterController.enabled = true;
            }
        }
    }

    private void InitializeTutorialBombListener()
    {
        _sceneBomb = FindAnyObjectByType<Bomb>();
        if (_sceneBomb != null)
        {
            _sceneBomb.State.OnValueChanged += HandleBombStateChanged;
            if (_sceneBomb.State.Value == BombState.Carried)
            {
                SetState(TutorialState.MoveToSideA);
            }
            else
            {
                SetState(TutorialState.PickUpBomb);
            }
        }
        else
        {
            SetState(TutorialState.PickUpBomb);
        }
    }

    private void HandleBombStateChanged(BombState previousState, BombState currentState)
    {
        switch (currentState)
        {
            case BombState.Carried:
                if (_currentState == TutorialState.PickUpBomb)
                {
                    SetState(TutorialState.MoveToSideA);
                }
                break;

            case BombState.Planted:
                if (_currentState == TutorialState.BombPlanting || _currentState == TutorialState.MoveToSideA)
                {
                    SetState(TutorialState.BombDefusing);
                    SwitchPlayerToDefuseTeam();
                }
                break;

            case BombState.Defused:
                if (_currentState == TutorialState.BombDefusing)
                {
                    SetState(TutorialState.PingSystem);
                }
                break;
        }
    }

    private void SwitchPlayerToDefuseTeam()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
        {
            PlayerTeam playerTeam = localPlayer.GetComponent<PlayerTeam>();
            if (playerTeam != null)
            {
                playerTeam.CurrentTeam.Value = Team.Blue;
            }
        }
    }

    private void SetState(TutorialState newState)
    {
        _currentState = newState;
        UpdateInstructionText();
    }

    private void UpdateInstructionText()
    {
        if (_instructionText == null) return;

        switch (_currentState)
        {
            case TutorialState.Welcome:
                _instructionText.text = "Rushpoint Tactical Protocol initialized.";
                break;

            case TutorialState.PickUpBomb:
                _instructionText.text = "Walk over the C4 Bomb on the floor to pick it up.";
                break;

            case TutorialState.MoveToSideA:
                _instructionText.text = "Proceed to Site A to initialize plant protocol.";
                break;

            case TutorialState.BombPlanting:
                _instructionText.text = "Inside Site A: Hold interact key looking at the ground to plant.";
                break;

            case TutorialState.BombDefusing:
                _instructionText.text = "Approach the planted C4 and hold interact key to defuse it.";
                break;

            case TutorialState.PingSystem:
                _instructionText.text = "Tactical Ping required: Place Normal, Danger, and Group pings using MMB.";
                break;

            case TutorialState.RoleSwapping:
                _instructionText.text = "Locate the equipment terminal and switch class to Medic.";
                break;

            case TutorialState.MedicTraining:
                _instructionText.text = "Draw your Healing Pistol and fire until the target unit is at 100%.";
                break;

            case TutorialState.Completed:
                _instructionText.text = "Training sequence complete. Ready for competitive operations.";
                break;
        }
    }

    public void OnEnteredSideA()
    {
        if (_currentState == TutorialState.MoveToSideA)
        {
            SetState(TutorialState.BombPlanting);
        }
    }

    public void OnPingPlaced(int pingType)
    {
        if (_currentState != TutorialState.PingSystem) return;

        if (pingType == 0) _hasPingNormal = true;
        if (pingType == 1) _hasPingDanger = true;
        if (pingType == 2) _hasPingGroup = true;

        if (_hasPingNormal && _hasPingDanger && _hasPingGroup)
        {
            SetState(TutorialState.RoleSwapping);
        }
    }

    public void OnRoleSwappedTo(string roleName)
    {
        if (_currentState != TutorialState.RoleSwapping) return;

        if (roleName == "Medic")
        {
            SetState(TutorialState.MedicTraining);
        }
    }

    public void CheckMedicTrainingProgress(float currentTargetHealth)
    {
        if (_currentState != TutorialState.MedicTraining) return;

        if (currentTargetHealth >= 100f)
        {
            SetState(TutorialState.Completed);
        }
    }
}