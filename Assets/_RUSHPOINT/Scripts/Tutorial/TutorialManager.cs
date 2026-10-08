using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
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
    [Tooltip("Prefab de tu jugador (NetworkPlayerController + NetworkObject).")]
    [SerializeField] private NetworkObject _playerPrefab;

    [Header("Medic training dummy")]
    [SerializeField] private float _dummyDistance = 6f;
    [SerializeField] private float _dummyStartHealth = 40f;

    [Header("Finish")]
    [SerializeField] private string _mainMenuSceneName = "MainMenuScene";
    [SerializeField] private float _finishDelay = 4f;

    [Header("Local network (tutorial)")]
    [SerializeField] private string _localAddress = "127.0.0.1";
    [SerializeField] private ushort _localPort = 7777;

    [Header("Ping inputs (paso PingSystem)")]
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
    private NetworkObject _dummyPlayer;
    private NetworkHealth _dummyHealth;
    private bool _dummySpawning;

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

        if (_dummyHealth != null)
        {
            _dummyHealth.CurrentHealth.OnValueChanged -= HandleDummyHealthChanged;
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

    private void StartLocalSession()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsListening) return;

        UnityTransport transport = nm.NetworkConfig != null
            ? nm.NetworkConfig.NetworkTransport as UnityTransport
            : null;

        if (transport != null)
        {
            transport.UseWebSockets = false;
            transport.UseEncryption = false;
            transport.SetConnectionData(_localAddress, _localPort);
        }

        PreparePlayerPrefabWithoutAutoSpawn(nm);
        nm.StartHost();
    }

    private void PreparePlayerPrefabWithoutAutoSpawn(NetworkManager nm)
    {
        _runtimePlayerPrefab = _playerPrefab != null
            ? _playerPrefab.gameObject
            : nm.NetworkConfig.PlayerPrefab;

        if (_runtimePlayerPrefab == null) return;

        if (!nm.NetworkConfig.Prefabs.Contains(_runtimePlayerPrefab))
        {
            nm.AddNetworkPrefab(_runtimePlayerPrefab);
        }

        nm.NetworkConfig.PlayerPrefab = null;
    }

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
            _spawnRoutine = null;
            OpenRoleSelector();
            yield break;
        }

        if (nm.SpawnManager.GetLocalPlayerObject() == null)
        {
            Vector3 position = _terroristSpawnPoint != null ? _terroristSpawnPoint.position : Vector3.zero;
            Quaternion safeYawRotation = _terroristSpawnPoint != null
                ? Quaternion.Euler(0f, _terroristSpawnPoint.eulerAngles.y, 0f)
                : Quaternion.identity;

            GameObject instance = Instantiate(_runtimePlayerPrefab, position, safeYawRotation);
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Destroy(instance);
                _spawnRoutine = null;
                OpenRoleSelector();
                yield break;
            }

            networkObject.SpawnAsPlayerObject(nm.LocalClientId, true);
        }

        ApplyRoleToLocalPlayer(role);
        PositionAndAssignTeam();

        if (_tacticalHudCanvas != null) _tacticalHudCanvas.SetActive(true);
        if (_minimapCanvas != null) _minimapCanvas.SetActive(true);

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

            Quaternion safeYawRotation = Quaternion.Euler(0f, _terroristSpawnPoint.eulerAngles.y, 0f);

            localPlayer.transform.position = _terroristSpawnPoint.position;
            localPlayer.transform.rotation = safeYawRotation;

            NetworkPlayerController controller = localPlayer.GetComponent<NetworkPlayerController>();
            if (controller != null)
            {
                Transform cameraRoot = GetPrivateField<Transform>(controller, "_cameraRoot");
                if (cameraRoot != null)
                {
                    cameraRoot.localRotation = Quaternion.identity;
                }

                FieldInfo pitchField = typeof(NetworkPlayerController).GetField(
                    "_cameraPitch",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (pitchField != null)
                {
                    pitchField.SetValue(controller, 0f);
                }

                if (controller.PlayerCamera != null)
                {
                    controller.PlayerCamera.transform.localRotation = Quaternion.identity;
                }
            }

            if (characterController != null) characterController.enabled = true;
        }

        GameObject lobbyCamera = GameObject.FindWithTag("LobbyCamera");
        if (lobbyCamera != null)
        {
            lobbyCamera.SetActive(false);
        }
    }

    private void InitializeUiState()
    {
        _roleScreen = FindAnyObjectByType<RoleSelectScreenUI>(FindObjectsInactive.Include);
        if (_roleScreen != null)
        {
            _roleScreen.SetTutorialMode(true);
            _roleScreen.RoleConfirmed += HandleRoleConfirmed;
        }

        if (_tacticalHudCanvas != null) _tacticalHudCanvas.SetActive(false);
        if (_minimapCanvas != null) _minimapCanvas.SetActive(false);
    }

    public void OpenRoleSelector()
    {
        if (_roleSelecterPanel != null) _roleSelecterPanel.SetActive(true);

        if (_roleScreen != null)
        {
            _roleScreen.ForceOpen();
        }
    }

    private static void SetGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleRoleConfirmed(PlayerRoleType role, Team team)
    {
        if (_currentState == TutorialState.ChooseClass)
        {
            if (_spawnRoutine == null)
            {
                _spawnRoutine = StartCoroutine(SpawnPlayerAndStartRoutine(role));
            }
            return;
        }

        ApplyRoleToLocalPlayer(role);
        OnRoleSwappedTo(role.ToString());
    }

    private void ApplyRoleToLocalPlayer(PlayerRoleType role)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;

        NetworkObject localPlayer = nm.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null) return;

        NetworkPlayerController controller = localPlayer.GetComponent<NetworkPlayerController>();
        if (controller != null)
        {
            controller.SetInitialRole(role);
        }
    }

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

    private void SetState(TutorialState newState)
    {
        _currentState = newState;
        UpdateInstructionText();

        if (newState == TutorialState.Completed)
        {
            Invoke(nameof(DespawnTrainingDummy), 2f);
            StartCoroutine(FinishTutorialRoutine());
        }
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
                _instructionText.text = "Draw your Healing Pistol and heal the target unit.";
                break;
            case TutorialState.Completed:
                _instructionText.text = "FINISHED\nTraining sequence complete. Returning to main menu...";
                break;
        }
    }

    private void DetectPingInput()
    {
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
            SwitchToMedicNow();
        }
        else
        {
            UpdateInstructionText();
        }
    }

    private void SwitchToMedicNow()
    {
        ApplyRoleToLocalPlayer(PlayerRoleType.Medic);
        SetState(TutorialState.MedicTraining);
        SetGameplayCursor();

        StartCoroutine(SpawnTrainingDummyRoutine());
    }

    private IEnumerator SpawnTrainingDummyRoutine()
    {
        if (_dummyPlayer != null || _dummySpawning) yield break;
        _dummySpawning = true;

        NetworkManager nm = NetworkManager.Singleton;
        NetworkObject localPlayer = nm != null && nm.IsServer
            ? nm.SpawnManager.GetLocalPlayerObject()
            : null;

        if (localPlayer == null || _runtimePlayerPrefab == null)
        {
            _dummySpawning = false;
            yield break;
        }

        TryGetDummySpawnPose(localPlayer.transform, out Vector3 position, out Quaternion rotation);

        GameObject holder = new GameObject("DummyHolder");
        holder.SetActive(false);

        GameObject dummy = Instantiate(_runtimePlayerPrefab, position, rotation, holder.transform);
        dummy.name = "TutorialDummy";

        NeutralizeDummy(dummy);

        dummy.transform.SetParent(null, true);
        Destroy(holder);

        NetworkObject networkObject = dummy.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Destroy(dummy);
            _dummySpawning = false;
            yield break;
        }

        networkObject.Spawn(true);
        _dummyPlayer = networkObject;

        yield return null;

        PlayerTeam localTeam = localPlayer.GetComponent<PlayerTeam>();
        PlayerTeam dummyTeam = dummy.GetComponent<PlayerTeam>();
        if (localTeam != null && dummyTeam != null)
        {
            dummyTeam.CurrentTeam.Value = localTeam.CurrentTeam.Value;
        }

        _dummyHealth = dummy.GetComponent<NetworkHealth>();
        if (_dummyHealth != null)
        {
            _dummyHealth.CurrentHealth.Value = _dummyStartHealth;
            _dummyHealth.CurrentHealth.OnValueChanged += HandleDummyHealthChanged;
        }

        _dummySpawning = false;
    }

    private IEnumerator FinishTutorialRoutine()
    {
        yield return new WaitForSecondsRealtime(_finishDelay);

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();

            float timeout = 3f;
            while (nm != null && nm.ShutdownInProgress && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(_mainMenuSceneName);
    }

    public bool OnTrainingDummyHit(Collider collider)
    {
        if (_dummyPlayer == null || collider == null) return false;

        NetworkObject hitObject = collider.GetComponentInParent<NetworkObject>();
        if (hitObject != _dummyPlayer) return false;

        if (_currentState == TutorialState.MedicTraining)
        {
            CheckMedicTrainingProgress(100f);
        }

        return true;
    }

    private void HandleDummyHealthChanged(float previousHealth, float currentHealth)
    {
        if (currentHealth > previousHealth)
        {
            CheckMedicTrainingProgress(100f);
        }
    }

    private static void NeutralizeDummy(GameObject dummy)
    {
        NetworkPlayerController controller = dummy.GetComponent<NetworkPlayerController>();
        if (controller != null)
        {
            GameObject firstPerson = GetPrivateField<GameObject>(controller, "_firstPersonRoot");
            GameObject thirdPerson = GetPrivateField<GameObject>(controller, "_thirdPersonRoot");
            Camera camera = GetPrivateField<Camera>(controller, "_playerCamera");
            AudioListener listener = GetPrivateField<AudioListener>(controller, "_audioListener");

            if (firstPerson != null) firstPerson.SetActive(false);
            if (thirdPerson != null) thirdPerson.SetActive(true);
            if (camera != null) camera.gameObject.SetActive(false);
            if (listener != null) listener.enabled = false;

            DestroyImmediate(controller);
        }

        UnityEngine.InputSystem.PlayerInput playerInput = dummy.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) DestroyImmediate(playerInput);

        Canvas ownCanvas = dummy.GetComponentInChildren<Canvas>(true);
        if (ownCanvas != null) ownCanvas.gameObject.SetActive(false);

        foreach (MonoBehaviour behaviour in dummy.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            if (behaviour is NetworkObject || behaviour is NetworkTransform) continue;
            if (behaviour is NetworkHealth || behaviour is PlayerTeam) continue;
            if (!string.IsNullOrEmpty(behaviour.GetType().Namespace)) continue;

            behaviour.enabled = false;
        }
    }

    private static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return field != null ? field.GetValue(target) as T : null;
    }

    private void TryGetDummySpawnPose(Transform player, out Vector3 position, out Quaternion rotation)
    {
        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

        Vector3[] directions =
        {
            forward,
            Quaternion.Euler(0f, 90f, 0f) * forward,
            Quaternion.Euler(0f, -90f, 0f) * forward,
            -forward
        };

        position = player.position + forward * _dummyDistance;
        bool found = false;

        foreach (Vector3 direction in directions)
        {
            Vector3 candidate = player.position + direction * _dummyDistance;
            Vector3 rayOrigin = new Vector3(candidate.x, player.position.y + 1.5f, candidate.z);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            candidate.y = hit.point.y;

            bool blocked = Physics.CheckCapsule(
                candidate + Vector3.up * 0.5f,
                candidate + Vector3.up * 1.6f,
                0.4f, ~0, QueryTriggerInteraction.Ignore);

            if (!blocked)
            {
                position = candidate;
                found = true;
                break;
            }
        }

        Vector3 toPlayer = player.position - position;
        toPlayer.y = 0f;
        rotation = toPlayer.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
    }

    private void DespawnTrainingDummy()
    {
        if (_dummyHealth != null)
        {
            _dummyHealth.CurrentHealth.OnValueChanged -= HandleDummyHealthChanged;
            _dummyHealth = null;
        }

        if (_dummyPlayer != null && _dummyPlayer.IsSpawned)
        {
            _dummyPlayer.Despawn(true);
        }

        _dummyPlayer = null;
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