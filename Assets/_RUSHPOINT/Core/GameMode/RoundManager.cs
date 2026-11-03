using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RoundPhase : byte
{
    WaitingForPlayers,
    Warmup,
    InProgress,
    RoundEnd,
    MatchEnd
}

public enum Team : byte
{
    Neutral,
    Red,
    Blue
}

[RequireComponent(typeof(NetworkObject))]
public class RoundManager : NetworkBehaviour
{
    private const int DEFAULT_ROUNDS_TO_WIN = 13;
    private const float DEFAULT_WARMUP_DURATION = 7f;
    private const float DEFAULT_ROUND_TIME_LIMIT = 115f;
    private const float DEFAULT_ROUND_END_DISPLAY_DURATION = 5f;
    private const float MATCH_END_STAND_DELAY_SECONDS = 10f;
    private const float BOMB_SPAWN_GROUND_OFFSET_Y = 0.2f;
    private const float BOOTSTRAP_TIMEOUT_SECONDS = 3f;
    private const float SERVER_INIT_WAIT_SECONDS = 0.5f;
    private const float WATCHDOG_THRESHOLD_SECONDS = 1.5f;
    private const string LOBBY_UI_NAME = "Ui_Lobby";
    private const string ROLE_CANVAS_NAME = "RoleSelectCanvas";
    private const string SPECTATOR_WAIT_CANVAS_NAME = "SpectatorWaitCanvas";
    private const string MAP_SCENE_NAME = "RUSPOINT";
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    private static RoundManager _instance;
    public static RoundManager Instance => _instance;

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Bomb _bomb;
    [SerializeField] private Transform[] _terroristSpawnPoints;
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;
    [SerializeField] private Transform _defaultBombSpawn;

    [SerializeField] private float _warmupDuration = DEFAULT_WARMUP_DURATION;
    [SerializeField] private float _roundTimeLimit = DEFAULT_ROUND_TIME_LIMIT;
    [SerializeField] private float _roundEndDisplayDuration = DEFAULT_ROUND_END_DISPLAY_DURATION;
    [SerializeField] private int _roundsToWinMatch = DEFAULT_ROUNDS_TO_WIN;

    private readonly NetworkVariable<RoundPhase> _currentPhase = new NetworkVariable<RoundPhase>(
        RoundPhase.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<double> _phaseStartServerTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _currentRoundTime = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _redScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _blueScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _roundNumber = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<Team> _lastRoundWinner = new NetworkVariable<Team>(
        Team.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<bool> _isBombPlanted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<RoundPhase> CurrentPhase => _currentPhase;
    public NetworkVariable<double> PhaseStartServerTime => _phaseStartServerTime;
    public NetworkVariable<int> CurrentRoundTime => _currentRoundTime;
    public NetworkVariable<int> RedScore => _redScore;
    public NetworkVariable<int> BlueScore => _blueScore;
    public NetworkVariable<int> RoundNumber => _roundNumber;
    public NetworkVariable<Team> LastRoundWinner => _lastRoundWinner;
    public NetworkVariable<bool> IsBombPlanted => _isBombPlanted;
    public NetworkVariable<int> TerroristScore => _redScore;
    public NetworkVariable<int> CounterTerroristScore => _blueScore;

    private int _clientSyncedTime;
    private int _clientSyncedRedScore;
    private int _clientSyncedBlueScore;
    private int _clientSyncedRoundNumber;
    private RoundPhase _clientSyncedPhase = RoundPhase.WaitingForPlayers;
    private Team _clientSyncedWinner = Team.Neutral;
    private bool _hasClientSyncedState;

    public int SyncedRoundTime => (_hasClientSyncedState && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) ? _clientSyncedTime : _currentRoundTime.Value;
    public int SyncedRedScore => (_hasClientSyncedState && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) ? _clientSyncedRedScore : _redScore.Value;
    public int SyncedBlueScore => (_hasClientSyncedState && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) ? _clientSyncedBlueScore : _blueScore.Value;
    public int SyncedRoundNumber => (_hasClientSyncedState && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) ? _clientSyncedRoundNumber : _roundNumber.Value;
    public RoundPhase SyncedPhase => (_hasClientSyncedState && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) ? _clientSyncedPhase : _currentPhase.Value;
    public Team SyncedLastWinner => (_hasClientSyncedState && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) ? _clientSyncedWinner : _lastRoundWinner.Value;

    public float RoundTimeLimit => _roundTimeLimit;
    public float WarmupDuration => _warmupDuration;
    public float RoundEndDisplayDuration => _roundEndDisplayDuration;
    public int RoundsToWinMatch => _roundsToWinMatch;
    public int HalftimeRound => Mathf.Max(2, _roundsToWinMatch);

    private Coroutine _roundLoopCoroutine;
    private bool _hasStartedMatch;
    private bool _hasSwitchedSides;
    private float _serverWatchdogTimer;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        CleanSceneArtifacts();
    }

    private void Start()
    {
        CleanSceneArtifacts();
        StartCoroutine(LocalEnsureScreenClearRoutine());
        StartCoroutine(ServerBootstrapRoutine());
    }

    private void Update()
    {
        if (!_hasStartedMatch && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f;

            _serverWatchdogTimer += Time.unscaledDeltaTime;
            if (_serverWatchdogTimer >= WATCHDOG_THRESHOLD_SECONDS)
            {
                _serverWatchdogTimer = 0f;
                PullLobbySettingsServer();
                StartMatchServer();
            }
        }
    }

    public void ApplyClientSyncedState(int roundTime, int redScore, int blueScore, int roundNumber, RoundPhase phase, Team lastWinner)
    {
        _hasClientSyncedState = true;
        _clientSyncedTime = roundTime;
        _clientSyncedRedScore = redScore;
        _clientSyncedBlueScore = blueScore;
        _clientSyncedRoundNumber = roundNumber;
        _clientSyncedPhase = phase;
        _clientSyncedWinner = lastWinner;

        CleanSceneArtifacts();
        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Dismiss();
        }

        if (!IsSpawned)
        {
            try
            {
                _currentRoundTime.Value = roundTime;
                _redScore.Value = redScore;
                _blueScore.Value = blueScore;
                _roundNumber.Value = roundNumber;
                _currentPhase.Value = phase;
                _lastRoundWinner.Value = lastWinner;
            }
            catch { }
        }
    }

    private void BroadcastStateToClients()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            if (clientPair.Value.PlayerObject != null)
            {
                NetworkPlayerController controller = clientPair.Value.PlayerObject.GetComponent<NetworkPlayerController>();
                if (controller != null && controller.IsSpawned)
                {
                    controller.SyncRoundStateClientRpc(
                        _currentRoundTime.Value,
                        _redScore.Value,
                        _blueScore.Value,
                        _roundNumber.Value,
                        (byte)_currentPhase.Value,
                        (byte)_lastRoundWinner.Value
                    );
                    break;
                }
            }
        }
    }

    private void CleanSceneArtifacts()
    {
        Time.timeScale = 1f;

        GameObject lobbyUi = GameObject.Find(LOBBY_UI_NAME);
        if (lobbyUi != null) lobbyUi.SetActive(false);

        GameObject roleCanvas = GameObject.Find(ROLE_CANVAS_NAME);
        if (roleCanvas != null) roleCanvas.SetActive(false);

        GameObject spectatorWaitCanvas = GameObject.Find(SPECTATOR_WAIT_CANVAS_NAME);
        if (spectatorWaitCanvas != null) spectatorWaitCanvas.SetActive(false);
    }

    private IEnumerator LocalEnsureScreenClearRoutine()
    {
        yield return new WaitForSecondsRealtime(SERVER_INIT_WAIT_SECONDS);
        CleanSceneArtifacts();

        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Dismiss();
        }
    }

    private IEnumerator ServerBootstrapRoutine()
    {
        float waitingElapsedTime = 0f;
        while ((NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) && waitingElapsedTime < BOOTSTRAP_TIMEOUT_SECONDS)
        {
            waitingElapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(SERVER_INIT_WAIT_SECONDS);
        PullLobbySettingsServer();
        StartMatchServer();
    }

    public override void OnNetworkSpawn()
    {
        _instance = this;
        CleanSceneArtifacts();

        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Dismiss();
        }

        if (IsServer)
        {
            PullLobbySettingsServer();

            if (_bomb == null) _bomb = FindAnyObjectByType<Bomb>();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleClientLoadComplete;
                NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleClientLoadComplete;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleClientLoadComplete;
            }

            if (_roundLoopCoroutine != null) StopCoroutine(_roundLoopCoroutine);
        }

        if (_instance == this) _instance = null;
    }

    private void HandleClientLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || sceneName != MAP_SCENE_NAME) return;

        if (!_hasStartedMatch)
        {
            PullLobbySettingsServer();
            StartMatchServer();
        }
        else
        {
            BroadcastStateToClients();
        }
    }

    private void PullLobbySettingsServer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (CustomLobbyManager.SavedRoundDuration > 0f)
        {
            _roundTimeLimit = CustomLobbyManager.SavedRoundDuration;
        }

        if (CustomLobbyManager.SavedRoundsToWin > 0)
        {
            _roundsToWinMatch = CustomLobbyManager.SavedRoundsToWin;
        }
    }

    public void ConfigureMatchSettingsServer(float roundDuration, int roundsToWin)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        _roundTimeLimit = roundDuration;
        _roundsToWinMatch = roundsToWin;
    }

    public void StartMatchServer()
    {
        if (_hasStartedMatch || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        _hasStartedMatch = true;
        _hasSwitchedSides = false;

        PullLobbySettingsServer();

        _redScore.Value = 0;
        _blueScore.Value = 0;
        _roundNumber.Value = 0;
        _lastRoundWinner.Value = Team.Neutral;

        CleanSceneArtifacts();
        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Dismiss();
        }

        if (_roundLoopCoroutine != null) StopCoroutine(_roundLoopCoroutine);
        _roundLoopCoroutine = StartCoroutine(MatchLoopRoutine());
    }

    public void NotifyBombStateChangedServer(BombState currentState)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _currentPhase.Value != RoundPhase.InProgress) return;

        if (currentState == BombState.Planted) _isBombPlanted.Value = true;
        else if (currentState == BombState.Defused) EndRoundServer(Team.Blue);
        else if (currentState == BombState.Exploded) EndRoundServer(Team.Red);
    }

    private IEnumerator MatchLoopRoutine()
    {
        while (_redScore.Value < _roundsToWinMatch && _blueScore.Value < _roundsToWinMatch)
        {
            _roundNumber.Value++;

            if (_roundNumber.Value == HalftimeRound && !_hasSwitchedSides)
            {
                _hasSwitchedSides = true;
                ExecuteHalftimeSideSwitch();
            }

            yield return StartCoroutine(WarmupRoutine());
            yield return StartCoroutine(InProgressRoutine());
            yield return StartCoroutine(RoundEndRoutine());
        }

        _currentPhase.Value = RoundPhase.MatchEnd;
        _phaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        BroadcastStateToClients();

        yield return new WaitForSeconds(MATCH_END_STAND_DELAY_SECONDS);

        ReturnToMainMenuFromServer();
    }

    private void ReturnToMainMenuFromServer()
    {
        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            if (clientPair.Value.PlayerObject != null)
            {
                NetworkPlayerController controller = clientPair.Value.PlayerObject.GetComponent<NetworkPlayerController>();
                if (controller != null && controller.IsSpawned)
                {
                    controller.ReturnToMainMenuClientRpc();
                    return;
                }
            }
        }

        StartCoroutine(DisconnectAndLoadMenuRoutine());
    }

    public void TriggerDisconnectAndLoadMenu()
    {
        StartCoroutine(DisconnectAndLoadMenuRoutine());
    }

    private void ExecuteHalftimeSideSwitch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        int cachedRedScore = _redScore.Value;
        _redScore.Value = _blueScore.Value;
        _blueScore.Value = cachedRedScore;

        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;
            NetworkClient client = clientPair.Value;

            if (CustomLobbyManager.SavedTeams != null && CustomLobbyManager.SavedTeams.TryGetValue(clientId, out Team currentSavedTeam))
            {
                if (currentSavedTeam == Team.Red) CustomLobbyManager.SavedTeams[clientId] = Team.Blue;
                else if (currentSavedTeam == Team.Blue) CustomLobbyManager.SavedTeams[clientId] = Team.Red;
            }

            if (client.PlayerObject == null) continue;

            PlayerTeam playerTeamComponent = client.PlayerObject.GetComponent<PlayerTeam>();
            if (playerTeamComponent == null) playerTeamComponent = client.PlayerObject.GetComponentInChildren<PlayerTeam>();
            if (playerTeamComponent != null)
            {
                Team previousTeam = playerTeamComponent.CurrentTeam.Value;
                if (previousTeam == Team.Red) playerTeamComponent.CurrentTeam.Value = Team.Blue;
                else if (previousTeam == Team.Blue) playerTeamComponent.CurrentTeam.Value = Team.Red;
            }
        }

        BroadcastStateToClients();
    }

    private IEnumerator DisconnectAndLoadMenuRoutine()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CustomLobbyManager.ClearLobbyData();

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            while (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
            {
                yield return null;
            }

            if (NetworkManager.Singleton != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
    }

    private IEnumerator WarmupRoutine()
    {
        _currentPhase.Value = RoundPhase.Warmup;
        _phaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        _currentRoundTime.Value = Mathf.CeilToInt(_warmupDuration);
        _isBombPlanted.Value = false;

        ResetEntitiesForNewRound();
        BroadcastStateToClients();

        int lastSecondReported = -1;

        while (true)
        {
            double elapsedDuration = GetPhaseElapsed();
            int remainingSeconds = Mathf.CeilToInt(Mathf.Max(_warmupDuration - (float)elapsedDuration, 0f));

            if (remainingSeconds != lastSecondReported)
            {
                lastSecondReported = remainingSeconds;
                _currentRoundTime.Value = remainingSeconds;
                BroadcastStateToClients();
            }

            if (elapsedDuration >= _warmupDuration) break;
            yield return null;
        }
    }

    private IEnumerator InProgressRoutine()
    {
        _currentPhase.Value = RoundPhase.InProgress;
        _phaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        _currentRoundTime.Value = Mathf.CeilToInt(_roundTimeLimit);
        BroadcastStateToClients();

        int lastSecondReported = -1;

        while (_currentPhase.Value == RoundPhase.InProgress)
        {
            if (_bomb == null) _bomb = FindAnyObjectByType<Bomb>();

            bool isBombActive = _bomb != null && (_bomb.CurrentState == BombState.Planted || _bomb.CurrentState == BombState.Defusing);

            if (isBombActive)
            {
                if (_bomb.PlantedTime > 0)
                {
                    double elapsedDetonation = NetworkManager.Singleton.ServerTime.Time - _bomb.PlantedTime;
                    int bombRemainingSeconds = Mathf.CeilToInt(Mathf.Max(_bomb.DetonationTimeDuration - (float)elapsedDetonation, 0f));

                    if (bombRemainingSeconds != lastSecondReported)
                    {
                        lastSecondReported = bombRemainingSeconds;
                        _currentRoundTime.Value = bombRemainingSeconds;
                        BroadcastStateToClients();
                    }

                    if (elapsedDetonation >= _bomb.DetonationTimeDuration)
                    {
                        _bomb.ServerExplode();
                        EndRoundServer(Team.Red);
                        break;
                    }
                }

                CheckEliminationsAfterPlant();
            }
            else
            {
                double elapsedDuration = GetPhaseElapsed();
                int roundRemainingSeconds = Mathf.CeilToInt(Mathf.Max(_roundTimeLimit - (float)elapsedDuration, 0f));

                if (roundRemainingSeconds != lastSecondReported)
                {
                    lastSecondReported = roundRemainingSeconds;
                    _currentRoundTime.Value = roundRemainingSeconds;
                    BroadcastStateToClients();
                }

                if (elapsedDuration >= _roundTimeLimit)
                {
                    EndRoundServer(Team.Blue);
                    break;
                }

                CheckEliminationsBeforePlant();
            }

            yield return null;
        }
    }

    private IEnumerator RoundEndRoutine()
    {
        _currentPhase.Value = RoundPhase.RoundEnd;
        _phaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        BroadcastStateToClients();

        int lastSecondReported = -1;

        while (true)
        {
            double elapsedDuration = GetPhaseElapsed();
            int remainingSeconds = Mathf.CeilToInt(Mathf.Max(_roundEndDisplayDuration - (float)elapsedDuration, 0f));

            if (remainingSeconds != lastSecondReported)
            {
                lastSecondReported = remainingSeconds;
                _currentRoundTime.Value = remainingSeconds;
                BroadcastStateToClients();
            }

            if (elapsedDuration >= _roundEndDisplayDuration) break;
            yield return null;
        }
    }

    private void CheckEliminationsBeforePlant()
    {
        int totalRed = 0, totalBlue = 0, aliveRed = 0, aliveBlue = 0;

        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkClient client = clientPair.Value;
            if (client.PlayerObject == null) continue;

            PlayerTeam playerTeamComponent = client.PlayerObject.GetComponent<PlayerTeam>();
            if (playerTeamComponent == null) playerTeamComponent = client.PlayerObject.GetComponentInChildren<PlayerTeam>();

            NetworkHealth healthComponent = client.PlayerObject.GetComponent<NetworkHealth>();
            if (healthComponent == null) healthComponent = client.PlayerObject.GetComponentInChildren<NetworkHealth>();

            if (playerTeamComponent == null || healthComponent == null) continue;

            if (playerTeamComponent.CurrentTeam.Value == Team.Red)
            {
                totalRed++;
                if (healthComponent.IsAlive.Value && healthComponent.CurrentHealth.Value > 0f) aliveRed++;
            }
            else if (playerTeamComponent.CurrentTeam.Value == Team.Blue)
            {
                totalBlue++;
                if (healthComponent.IsAlive.Value && healthComponent.CurrentHealth.Value > 0f) aliveBlue++;
            }
        }

        if (totalRed > 0 && totalBlue > 0)
        {
            if (aliveRed <= 0 && aliveBlue > 0) EndRoundServer(Team.Blue);
            else if (aliveBlue <= 0 && aliveRed > 0) EndRoundServer(Team.Red);
        }
    }

    private void CheckEliminationsAfterPlant()
    {
        int totalBlue = 0, aliveBlue = 0;

        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkClient client = clientPair.Value;
            if (client.PlayerObject == null) continue;

            PlayerTeam playerTeamComponent = client.PlayerObject.GetComponent<PlayerTeam>();
            if (playerTeamComponent == null) playerTeamComponent = client.PlayerObject.GetComponentInChildren<PlayerTeam>();

            NetworkHealth healthComponent = client.PlayerObject.GetComponent<NetworkHealth>();
            if (healthComponent == null) healthComponent = client.PlayerObject.GetComponentInChildren<NetworkHealth>();

            if (playerTeamComponent == null || healthComponent == null) continue;

            if (playerTeamComponent.CurrentTeam.Value == Team.Blue)
            {
                totalBlue++;
                if (healthComponent.IsAlive.Value && healthComponent.CurrentHealth.Value > 0f) aliveBlue++;
            }
        }

        if (totalBlue > 0 && aliveBlue <= 0) EndRoundServer(Team.Red);
    }

    private void EndRoundServer(Team winner)
    {
        if (_currentPhase.Value != RoundPhase.InProgress) return;

        _lastRoundWinner.Value = winner;
        if (winner == Team.Red) _redScore.Value++;
        else if (winner == Team.Blue) _blueScore.Value++;

        _currentRoundTime.Value = 0;
        _currentPhase.Value = RoundPhase.RoundEnd;

        BroadcastStateToClients();
    }

    private void ResetEntitiesForNewRound()
    {
        if (NetworkManager.Singleton == null) return;

        bool anyCombatantExists = false;
        foreach (KeyValuePair<ulong, NetworkClient> kvp in NetworkManager.Singleton.ConnectedClients)
        {
            if (CustomLobbyManager.SavedTeams != null &&
                CustomLobbyManager.SavedTeams.TryGetValue(kvp.Key, out Team saved) &&
                (saved == Team.Red || saved == Team.Blue))
            {
                anyCombatantExists = true;
                break;
            }
        }

        int terroristSpawnCounter = 0;
        int counterTerroristSpawnCounter = 0;
        List<KeyValuePair<ulong, NetworkClient>> spectatorQueue = new List<KeyValuePair<ulong, NetworkClient>>();

        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;
            NetworkClient client = clientPair.Value;

            Team assignedTeam = Team.Red;
            if (CustomLobbyManager.SavedTeams != null && CustomLobbyManager.SavedTeams.TryGetValue(clientId, out Team savedTeam))
            {
                assignedTeam = savedTeam;
            }

            if (!anyCombatantExists && assignedTeam == Team.Neutral)
            {
                assignedTeam = (clientId % 2 == 0) ? Team.Red : Team.Blue;
            }

            if (assignedTeam == Team.Neutral)
            {
                spectatorQueue.Add(clientPair);
                continue;
            }

            try
            {
                Transform[] spawnCollection = (assignedTeam == Team.Red) ? _terroristSpawnPoints : _counterTerroristSpawnPoints;
                if (spawnCollection == null || spawnCollection.Length == 0) spawnCollection = _terroristSpawnPoints;

                int spawnIndex = (assignedTeam == Team.Red) ? terroristSpawnCounter++ : counterTerroristSpawnCounter++;
                Vector3 targetPosition = Vector3.up * 2f;
                Quaternion targetRotation = Quaternion.identity;

                if (spawnCollection != null && spawnCollection.Length > 0)
                {
                    Transform targetSpawn = spawnCollection[spawnIndex % spawnCollection.Length];
                    if (targetSpawn != null)
                    {
                        targetPosition = targetSpawn.position;
                        targetRotation = targetSpawn.rotation;
                    }
                }

                GameObject activePlayerObject = null;

                if (client.PlayerObject == null)
                {
                    GameObject prefabToSpawn = _playerPrefab;
                    if (prefabToSpawn == null && NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
                    {
                        prefabToSpawn = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                    }

                    if (prefabToSpawn != null)
                    {
                        activePlayerObject = Instantiate(prefabToSpawn, targetPosition, targetRotation);
                        NetworkObject netObj = activePlayerObject.GetComponent<NetworkObject>();
                        netObj.SpawnAsPlayerObject(clientId, true);
                    }
                }
                else
                {
                    activePlayerObject = client.PlayerObject.gameObject;
                }

                if (activePlayerObject != null)
                {
                    PlayerTeam playerTeamComp = activePlayerObject.GetComponent<PlayerTeam>();
                    if (playerTeamComp == null) playerTeamComp = activePlayerObject.GetComponentInChildren<PlayerTeam>();
                    if (playerTeamComp != null) playerTeamComp.CurrentTeam.Value = assignedTeam;

                    NetworkHealth health = activePlayerObject.GetComponent<NetworkHealth>();
                    if (health == null) health = activePlayerObject.GetComponentInChildren<NetworkHealth>();
                    if (health != null) health.ResetHealthServer();

                    NetworkPlayerController playerController = activePlayerObject.GetComponent<NetworkPlayerController>();

                    if (CustomLobbyManager.SavedRoles != null && CustomLobbyManager.SavedRoles.TryGetValue(clientId, out PlayerRoleType savedRole))
                    {
                        if (playerController != null) playerController.SetInitialRole(savedRole);
                    }

                    if (playerController != null)
                    {
                        playerController.RespawnPlayer(targetPosition, targetRotation);
                        playerController.TeleportAndResetClientRpc(targetPosition, targetRotation);
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[RoundManager] Failed to spawn combatant {clientId}: {exception.Message}");
            }
        }

        for (int i = 0; i < spectatorQueue.Count; i++)
        {
            HandleSpectatorClientServer(spectatorQueue[i].Key, spectatorQueue[i].Value);
        }

        if (_bomb == null) _bomb = FindAnyObjectByType<Bomb>();

        if (_bomb != null)
        {
            Vector3 bombPosition = (_defaultBombSpawn != null)
                ? _defaultBombSpawn.position
                : (_terroristSpawnPoints != null && _terroristSpawnPoints.Length > 0
                    ? _terroristSpawnPoints[0].position + (Vector3.up * BOMB_SPAWN_GROUND_OFFSET_Y)
                    : Vector3.up * BOMB_SPAWN_GROUND_OFFSET_Y);

            _bomb.ServerResetBomb(bombPosition);
        }
    }

    private void HandleSpectatorClientServer(ulong clientId, NetworkClient client)
    {
        try
        {
            GameObject spectatorObject = null;

            if (client.PlayerObject == null)
            {
                GameObject prefabToSpawn = _playerPrefab;
                if (prefabToSpawn == null && NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
                {
                    prefabToSpawn = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                }

                if (prefabToSpawn != null)
                {
                    Vector3 spectatorAnchor = new Vector3(0f, -50f, 0f);
                    spectatorObject = Instantiate(prefabToSpawn, spectatorAnchor, Quaternion.identity);
                    NetworkObject netObj = spectatorObject.GetComponent<NetworkObject>();
                    netObj.SpawnAsPlayerObject(clientId, true);
                }
            }
            else
            {
                spectatorObject = client.PlayerObject.gameObject;
            }

            if (spectatorObject != null)
            {
                PlayerTeam playerTeamComponent = spectatorObject.GetComponent<PlayerTeam>();
                if (playerTeamComponent == null) playerTeamComponent = spectatorObject.GetComponentInChildren<PlayerTeam>();
                if (playerTeamComponent != null) playerTeamComponent.CurrentTeam.Value = Team.Neutral;

                NetworkHealth healthComponent = spectatorObject.GetComponent<NetworkHealth>();
                if (healthComponent == null) healthComponent = spectatorObject.GetComponentInChildren<NetworkHealth>();
                if (healthComponent != null)
                {
                    healthComponent.SetSpectatorStateServer();
                }
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[RoundManager] Failed to configure spectator {clientId}: {exception.Message}");
        }
    }

    private double GetPhaseElapsed()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.ServerTime.Time - _phaseStartServerTime.Value;
    }
}