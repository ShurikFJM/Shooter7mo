using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum PingMouseButton
{
    Left,
    Right,
    Middle
}

public enum PingModifier
{
    None,
    Shift,
    Ctrl,
    Alt
}

/// <summary>Combinacion de input que identifica un tipo de ping.</summary>
[Serializable]
public class PingInputBinding
{
    public PingMouseButton button = PingMouseButton.Middle;
    public PingModifier modifier = PingModifier.None;

    public PingInputBinding() { }

    public PingInputBinding(PingMouseButton button, PingModifier modifier)
    {
        this.button = button;
        this.modifier = modifier;
    }

    public bool Matches(PingMouseButton pressedButton, PingModifier heldModifier)
    {
        return button == pressedButton && modifier == heldModifier;
    }

    public override string ToString()
    {
        string buttonName = button == PingMouseButton.Middle ? "MMB"
            : button == PingMouseButton.Left ? "LMB"
            : "RMB";

        return modifier == PingModifier.None ? buttonName : modifier + "+" + buttonName;
    }
}

public enum TutorialState
{
    Welcome,
    ChooseClass,
    PickUpBomb,
    MoveToSideA,
    BombPlanting,
    BombDefusing,
    PingSystem,
    RoleSwapping,
    MedicTraining,
    Completed
}

/// <summary>
/// Director unico del tutorial: arranca la red en local, controla la UI
/// (selector de roles, HUD, cursor), aplica el rol al jugador y decide el avance de pasos.
/// Los demas scripts solo le notifican eventos (OnEnteredSideA, OnPingPlaced, ...).
///
/// DefaultExecutionOrder alto: su Start corre DESPUES del de LobbyUI, que desactiva
/// el canvas del selector de roles al iniciar.
/// </summary>
[DefaultExecutionOrder(200)]
public class TutorialManager : NetworkBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _instructionText;
    [SerializeField] private GameObject _roleSelecterPanel;
    [SerializeField] private GameObject _tacticalHudCanvas;
    [SerializeField] private GameObject _minimapCanvas;

    [Header("Scene")]
    [SerializeField] private Transform _terroristSpawnPoint;

    [Header("Player spawn")]
    [Tooltip("Prefab de tu jugador (NetworkPlayerController + NetworkObject). Arrastra aqui el PREFAB, no un objeto de la escena. Se spawnea al confirmar la clase, no al iniciar.")]
    [SerializeField] private NetworkObject _playerPrefab;

    [Header("Local network (tutorial)")]
    [SerializeField] private string _localAddress = "127.0.0.1";
    [SerializeField] private ushort _localPort = 7777;

    [Header("Ping inputs (paso PingSystem)")]
    [Tooltip("Ajusta estas combinaciones a las de tu sistema de pings real.")]
    [SerializeField] private PingInputBinding _normalPingInput = new PingInputBinding(PingMouseButton.Middle, PingModifier.None);
    [SerializeField] private PingInputBinding _dangerPingInput = new PingInputBinding(PingMouseButton.Middle, PingModifier.Shift);
    [SerializeField] private PingInputBinding _groupPingInput = new PingInputBinding(PingMouseButton.Middle, PingModifier.Ctrl);

    private TutorialState _currentState;
    private Bomb _sceneBomb;
    private RoleSelectScreenUI _roleScreen;
    private bool _initialRoleChosen;
    private bool _hasPingNormal;
    private bool _hasPingDanger;
    private bool _hasPingGroup;
    private Coroutine _spawnRoutine;
    private GameObject _runtimePlayerPrefab;

    // ------------------------------------------------------------------ lifecycle

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
        StartLocalSession();

        // Primero hay que elegir clase para empezar.
        SetState(TutorialState.ChooseClass);
        OpenRoleSelector();

        Invoke(nameof(InitializeTutorialBombListener), 1.5f);
    }

    private void Update()
    {
        if (_currentState == TutorialState.PingSystem)
        {
            DetectPingInput();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (Instance == this)
        {
            Instance = null;
        }

        if (_sceneBomb != null)
        {
            _sceneBomb.State.OnValueChanged -= HandleBombStateChanged;
        }

        if (_roleScreen != null)
        {
            _roleScreen.RoleConfirmed -= HandleRoleConfirmed;
        }
    }

    // ------------------------------------------------------------------ network

    /// <summary>
    /// Arranca un host local. Fuerza el transporte a UDP directo (sin Relay ni WebSockets),
    /// que es lo que provocaba "You must call SetRelayServerData()".
    /// </summary>
    private void StartLocalSession()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[Tutorial] No hay NetworkManager en la escena.");
            return;
        }

        if (nm.IsListening)
        {
            Debug.Log("[Tutorial] La red ya estaba activa.");
            return;
        }

        UnityTransport transport = nm.NetworkConfig != null
            ? nm.NetworkConfig.NetworkTransport as UnityTransport
            : null;

        if (transport != null)
        {
            transport.UseWebSockets = false;
            transport.UseEncryption = false;
            // SetConnectionData tambien cambia el protocolo a "Unity Transport" (no Relay).
            transport.SetConnectionData(_localAddress, _localPort);
        }
        else
        {
            Debug.LogWarning("[Tutorial] El transporte no es UnityTransport; revisa su configuracion.");
        }

        PreparePlayerPrefabWithoutAutoSpawn(nm);

        bool started = nm.StartHost();
        Debug.Log($"[Tutorial] StartHost() => {started}");

        if (!started)
        {
            Debug.LogError("[Tutorial] StartHost() fallo. Revisa el UnityTransport del NetworkManager (Protocol Type = Unity Transport, Use Web Sockets desactivado).");
        }
    }

    // ------------------------------------------------------------------ player spawn

    /// <summary>
    /// Guarda tu prefab, lo registra en Network Prefabs y deja NetworkConfig.PlayerPrefab vacio
    /// para que Netcode NO spawnee el jugador al arrancar el host. Se spawnea al elegir clase.
    /// </summary>
    private void PreparePlayerPrefabWithoutAutoSpawn(NetworkManager nm)
    {
        _runtimePlayerPrefab = _playerPrefab != null
            ? _playerPrefab.gameObject
            : nm.NetworkConfig.PlayerPrefab;

        if (_runtimePlayerPrefab == null)
        {
            Debug.LogError("[Tutorial] No hay Player Prefab: asigna 'Player Prefab' en el TutorialManager.");
            return;
        }

        if (!nm.NetworkConfig.Prefabs.Contains(_runtimePlayerPrefab))
        {
            nm.AddNetworkPrefab(_runtimePlayerPrefab);
        }

        // Sin PlayerPrefab, StartHost() no crea ningun jugador.
        nm.NetworkConfig.PlayerPrefab = null;

        Debug.Log($"[Tutorial] Player Prefab '{_runtimePlayerPrefab.name}' listo; se spawneara al confirmar la clase.");
    }

    /// <summary>
    /// Se ejecuta al confirmar la primera clase: spawnea el jugador, le aplica el rol,
    /// lo coloca, muestra el HUD, bloquea el cursor y arranca el paso de la bomba.
    /// </summary>
    private IEnumerator SpawnPlayerAndStartRoutine(PlayerRoleType role)
    {
        NetworkManager nm = NetworkManager.Singleton;
        float timeout = 3f;

        while (nm != null && timeout > 0f &&
               !(nm.IsListening && nm.IsServer && nm.ConnectedClients.ContainsKey(nm.LocalClientId)))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        bool ready = nm != null && nm.IsListening && nm.IsServer &&
                     nm.ConnectedClients.ContainsKey(nm.LocalClientId);

        if (!ready || _runtimePlayerPrefab == null)
        {
            Debug.LogError("[Tutorial] No se pudo spawnear el jugador (red no lista o falta Player Prefab).");
            _spawnRoutine = null;
            OpenRoleSelector();
            yield break;
        }

        if (nm.SpawnManager.GetLocalPlayerObject() == null)
        {
            Vector3 position = _terroristSpawnPoint != null ? _terroristSpawnPoint.position : Vector3.zero;
            Quaternion rotation = _terroristSpawnPoint != null ? _terroristSpawnPoint.rotation : Quaternion.identity;

            GameObject instance = Instantiate(_runtimePlayerPrefab, position, rotation);
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("[Tutorial] El Player Prefab no tiene NetworkObject.");
                Destroy(instance);
                _spawnRoutine = null;
                OpenRoleSelector();
                yield break;
            }

            networkObject.SpawnAsPlayerObject(nm.LocalClientId, true);
            Debug.Log("[Tutorial] Jugador spawneado tras elegir clase.");
        }

        ApplyRoleToLocalPlayer(role);
        PositionAndAssignTeam();

        if (_tacticalHudCanvas != null) _tacticalHudCanvas.SetActive(true);
        if (_minimapCanvas != null) _minimapCanvas.SetActive(true);

        // OnNetworkSpawn del jugador libera el cursor: lo volvemos a bloquear.
        _initialRoleChosen = true;
        SetGameplayCursor();
        ApplyBombStep();

        _spawnRoutine = null;
    }

    private void PositionAndAssignTeam()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null) return;

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

    // ------------------------------------------------------------------ role / UI orders

    private void InitializeUiState()
    {
        // Include: encuentra el script aunque el Canvas este desactivado.
        _roleScreen = FindAnyObjectByType<RoleSelectScreenUI>(FindObjectsInactive.Include);
        if (_roleScreen != null)
        {
            _roleScreen.SetTutorialMode(true);
            _roleScreen.RoleConfirmed += HandleRoleConfirmed;
        }
        else
        {
            Debug.LogWarning("[Tutorial] No se encontro RoleSelectScreenUI en la escena.");
        }

        // El HUD no tiene sentido sin jugador: se activa al spawnearlo.
        if (_tacticalHudCanvas != null) _tacticalHudCanvas.SetActive(false);
        if (_minimapCanvas != null) _minimapCanvas.SetActive(false);
    }

    public void OpenRoleSelector()
    {
        if (_roleSelecterPanel != null) _roleSelecterPanel.SetActive(true);

        if (_roleScreen != null)
        {
            _roleScreen.ForceOpen();
            Debug.Log("[Tutorial] Selector de roles abierto.");
        }
    }

    private static void SetGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleRoleConfirmed(PlayerRoleType role, Team team)
    {
        Debug.Log($"[Tutorial] Rol confirmado: {role} (estado: {_currentState})");

        if (_currentState == TutorialState.ChooseClass)
        {
            // Primera clase: el jugador aun no existe, se spawnea ahora.
            if (_spawnRoutine == null)
            {
                _spawnRoutine = StartCoroutine(SpawnPlayerAndStartRoutine(role));
            }
            return;
        }

        ApplyRoleToLocalPlayer(role);
        OnRoleSwappedTo(role.ToString());
    }

    /// <summary>
    /// Aplica el rol directamente al jugador local (host), sin pasar por RoleLobbyManager.
    /// </summary>
    private void ApplyRoleToLocalPlayer(PlayerRoleType role)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
        {
            Debug.LogWarning("[Tutorial] Red inactiva: no se puede aplicar el rol al jugador.");
            return;
        }

        NetworkObject localPlayer = nm.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null)
        {
            Debug.LogWarning("[Tutorial] Todavia no hay jugador local spawneado.");
            return;
        }

        NetworkPlayerController controller = localPlayer.GetComponent<NetworkPlayerController>();
        if (controller != null)
        {
            controller.SetInitialRole(role);
        }
    }

    // ------------------------------------------------------------------ bomb

    private void InitializeTutorialBombListener()
    {
        _sceneBomb = FindAnyObjectByType<Bomb>();
        if (_sceneBomb != null)
        {
            _sceneBomb.State.OnValueChanged += HandleBombStateChanged;
        }

        if (_initialRoleChosen && _currentState == TutorialState.PickUpBomb)
        {
            ApplyBombStep();
        }
    }

    private void ApplyBombStep()
    {
        if (_sceneBomb != null && _sceneBomb.State.Value == BombState.Carried)
        {
            SetState(TutorialState.MoveToSideA);
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
        if (localPlayer == null) return;

        PlayerTeam playerTeam = localPlayer.GetComponent<PlayerTeam>();
        if (playerTeam != null)
        {
            playerTeam.CurrentTeam.Value = Team.Blue;
        }
    }

    // ------------------------------------------------------------------ state

    private void SetState(TutorialState newState)
    {
        _currentState = newState;
        Debug.Log($"[Tutorial] Estado => {newState}");
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
            case TutorialState.ChooseClass:
                _instructionText.text = "Select your class and press Confirm to begin.";
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
                _instructionText.text = BuildPingInstruction();
                break;
            case TutorialState.RoleSwapping:
                _instructionText.text = "Select the Medic class and press Confirm.";
                break;
            case TutorialState.MedicTraining:
                _instructionText.text = "Draw your Healing Pistol and fire until the target unit is at 100%.";
                break;
            case TutorialState.Completed:
                _instructionText.text = "Training sequence complete. Ready for competitive operations.";
                break;
        }
    }

    // ------------------------------------------------------------------ ping input

    /// <summary>
    /// Lee el input del jugador: si coincide con la combinacion de un tipo de ping
    /// que aun no se ha hecho, lo marca. Al completar los tres pasa a RoleSwapping.
    /// </summary>
    private void DetectPingInput()
    {
        // Con el selector abierto o el cursor libre no cuenta (el jugador esta en un menu).
        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (!TryGetMousePress(out PingMouseButton pressedButton)) return;

        PingModifier heldModifier = GetHeldModifier();

        if (_normalPingInput.Matches(pressedButton, heldModifier)) OnPingPlaced(0);
        else if (_dangerPingInput.Matches(pressedButton, heldModifier)) OnPingPlaced(1);
        else if (_groupPingInput.Matches(pressedButton, heldModifier)) OnPingPlaced(2);
    }

    private static bool TryGetMousePress(out PingMouseButton button)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.middleButton.wasPressedThisFrame) { button = PingMouseButton.Middle; return true; }
            if (mouse.leftButton.wasPressedThisFrame) { button = PingMouseButton.Left; return true; }
            if (mouse.rightButton.wasPressedThisFrame) { button = PingMouseButton.Right; return true; }
        }
#else
        if (Input.GetMouseButtonDown(2)) { button = PingMouseButton.Middle; return true; }
        if (Input.GetMouseButtonDown(0)) { button = PingMouseButton.Left; return true; }
        if (Input.GetMouseButtonDown(1)) { button = PingMouseButton.Right; return true; }
#endif
        button = default;
        return false;
    }

    private static PingModifier GetHeldModifier()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return PingModifier.None;

        if (keyboard.shiftKey.isPressed) return PingModifier.Shift;
        if (keyboard.ctrlKey.isPressed) return PingModifier.Ctrl;
        if (keyboard.altKey.isPressed) return PingModifier.Alt;
#else
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return PingModifier.Shift;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) return PingModifier.Ctrl;
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return PingModifier.Alt;
#endif
        return PingModifier.None;
    }

    private string BuildPingInstruction()
    {
        return "Tactical Ping required:\n" +
               "Normal (" + _normalPingInput + ")" + (_hasPingNormal ? " [x]" : "") + "\n" +
               "Danger (" + _dangerPingInput + ")" + (_hasPingDanger ? " [x]" : "") + "\n" +
               "Group (" + _groupPingInput + ")" + (_hasPingGroup ? " [x]" : "");
    }

    // ------------------------------------------------------------------ notifications (other scripts call these)

    public void OnEnteredSideA()
    {
        if (_currentState == TutorialState.MoveToSideA)
        {
            SetState(TutorialState.BombPlanting);
        }
    }

    /// <param name="pingType">0 = Normal, 1 = Danger, 2 = Group</param>
    public void OnPingPlaced(int pingType)
    {
        if (_currentState != TutorialState.PingSystem) return;

        if (pingType == 0) _hasPingNormal = true;
        if (pingType == 1) _hasPingDanger = true;
        if (pingType == 2) _hasPingGroup = true;

        Debug.Log($"[Tutorial] Ping {pingType} (N:{_hasPingNormal} D:{_hasPingDanger} G:{_hasPingGroup})");

        if (_hasPingNormal && _hasPingDanger && _hasPingGroup)
        {
            SwitchToMedicNow();
        }
        else
        {
            UpdateInstructionText();
        }
    }

    /// <summary>
    /// Cambia al jugador a Medic en el momento, sin abrir el selector de roles
    /// (asi no hay que pausar ni desactivar al jugador) y arranca el entrenamiento.
    /// </summary>
    private void SwitchToMedicNow()
    {
        ApplyRoleToLocalPlayer(PlayerRoleType.Medic);
        SetState(TutorialState.MedicTraining);
        SetGameplayCursor();
        Debug.Log("[Tutorial] Rol cambiado a Medic automaticamente.");
    }

    public void OnRoleSwappedTo(string roleName)
    {
        if (_currentState != TutorialState.RoleSwapping) return;

        if (roleName == "Medic")
        {
            SetState(TutorialState.MedicTraining);
            SetGameplayCursor();
        }
        else
        {
            if (_instructionText != null)
            {
                _instructionText.text = "Wrong class. Select the Medic class to continue.";
            }
            OpenRoleSelector();
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