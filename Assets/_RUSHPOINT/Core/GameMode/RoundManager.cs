using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
    private const float BOMB_SPAWN_GROUND_OFFSET_Y = 0.2f;

    public static RoundManager Instance { get; private set; }

    [Header("Prefabs y Spawns")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Bomb _bomb;
    [SerializeField] private Transform[] _terroristSpawnPoints;
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;
    [SerializeField] private Transform _defaultBombSpawn;

    [Header("Configuracion de Ronda")]
    [SerializeField] private float _warmupDuration = DEFAULT_WARMUP_DURATION;
    [SerializeField] private float _roundTimeLimit = DEFAULT_ROUND_TIME_LIMIT;
    [SerializeField] private float _roundEndDisplayDuration = DEFAULT_ROUND_END_DISPLAY_DURATION;
    [SerializeField] private int _roundsToWinMatch = DEFAULT_ROUNDS_TO_WIN;

    public NetworkVariable<RoundPhase> CurrentPhase = new NetworkVariable<RoundPhase>(
        RoundPhase.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<double> PhaseStartServerTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> CurrentRoundTime = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> RedScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> BlueScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> RoundNumber = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<Team> LastRoundWinner = new NetworkVariable<Team>(
        Team.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsBombPlanted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float RoundTimeLimit => _roundTimeLimit;
    public float WarmupDuration => _warmupDuration;
    public float RoundEndDisplayDuration => _roundEndDisplayDuration;
    public int RoundsToWinMatch => _roundsToWinMatch;
    public int HalftimeRound => Mathf.Max(1, _roundsToWinMatch - 1);
    public NetworkVariable<int> TerroristScore => RedScore;
    public NetworkVariable<int> CounterTerroristScore => BlueScore;

    private Coroutine _roundLoopCoroutine;
    private bool _hasStartedMatch;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CleanSceneArtifacts();
    }

    private void CleanSceneArtifacts()
    {
        Time.timeScale = 1f;

        // Oculta la UI residual de la lobby sin apagar Canvas_Pause
        GameObject lobbyUi = GameObject.Find("Ui_Lobby");
        if (lobbyUi != null)
        {
            lobbyUi.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CleanSceneArtifacts();

            if (_bomb == null) _bomb = FindAnyObjectByType<Bomb>();
            if (_bomb != null) _bomb.bombState.OnValueChanged += HandleBombStateChanged;

            StartCoroutine(ServerDelayedStartRoutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (_bomb != null) _bomb.bombState.OnValueChanged -= HandleBombStateChanged;
            if (_roundLoopCoroutine != null) StopCoroutine(_roundLoopCoroutine);
        }

        if (Instance == this) Instance = null;
    }

    private IEnumerator ServerDelayedStartRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        if (IsServer && IsSpawned && !_hasStartedMatch)
        {
            StartMatchServer();
        }
    }

    public void ConfigureMatchSettingsServer(float roundDuration, int roundsToWin)
    {
        if (!IsServer) return;
        _roundTimeLimit = roundDuration;
        _roundsToWinMatch = roundsToWin;
    }

    public void StartMatchServer()
    {
        if (_hasStartedMatch || !IsServer || !IsSpawned) return;

        _hasStartedMatch = true;

        RedScore.Value = 0;
        BlueScore.Value = 0;
        RoundNumber.Value = 0;
        LastRoundWinner.Value = Team.Neutral;

        DismissLoadingScreenClientRpc();

        if (_roundLoopCoroutine != null) StopCoroutine(_roundLoopCoroutine);
        _roundLoopCoroutine = StartCoroutine(MatchLoopRoutine());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DismissLoadingScreenClientRpc()
    {
        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Dismiss();
        }
    }

    private void HandleBombStateChanged(BombState previousState, BombState currentState)
    {
        if (!IsServer || CurrentPhase.Value != RoundPhase.InProgress) return;

        if (currentState == BombState.Planted) IsBombPlanted.Value = true;
        else if (currentState == BombState.Defused) EndRoundServer(Team.Blue);
        else if (currentState == BombState.Exploded) EndRoundServer(Team.Red);
    }

    private IEnumerator MatchLoopRoutine()
    {
        while (RedScore.Value < _roundsToWinMatch && BlueScore.Value < _roundsToWinMatch)
        {
            RoundNumber.Value++;

            if (RoundNumber.Value == HalftimeRound)
            {
                PerformHalftimeSideSwitch();
            }

            yield return StartCoroutine(WarmupRoutine());
            yield return StartCoroutine(InProgressRoutine());
            yield return StartCoroutine(RoundEndRoutine());
        }

        CurrentPhase.Value = RoundPhase.MatchEnd;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    private void PerformHalftimeSideSwitch()
    {
        if (!IsServer) return;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkClient client = clientPair.Value;
            if (client.PlayerObject == null) continue;

            PlayerTeam teamComp = client.PlayerObject.GetComponent<PlayerTeam>();
            if (teamComp == null) teamComp = client.PlayerObject.GetComponentInChildren<PlayerTeam>();
            if (teamComp != null)
            {
                Team oldTeam = teamComp.CurrentTeam.Value;
                if (oldTeam != Team.Neutral)
                {
                    teamComp.CurrentTeam.Value = (oldTeam == Team.Red) ? Team.Blue : Team.Red;
                }
            }
        }
    }

    private IEnumerator WarmupRoutine()
    {
        CurrentPhase.Value = RoundPhase.Warmup;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        CurrentRoundTime.Value = Mathf.CeilToInt(_warmupDuration);
        IsBombPlanted.Value = false;

        ResetEntitiesForNewRound();

        int lastSecondReported = -1;

        while (true)
        {
            double elapsed = GetPhaseElapsed();
            int remaining = Mathf.CeilToInt(Mathf.Max(_warmupDuration - (float)elapsed, 0f));

            if (remaining != lastSecondReported)
            {
                lastSecondReported = remaining;
                CurrentRoundTime.Value = remaining;
            }

            if (elapsed >= _warmupDuration) break;
            yield return null;
        }
    }

    private IEnumerator InProgressRoutine()
    {
        CurrentPhase.Value = RoundPhase.InProgress;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        CurrentRoundTime.Value = Mathf.CeilToInt(_roundTimeLimit);

        int lastSecondReported = -1;

        while (CurrentPhase.Value == RoundPhase.InProgress)
        {
            bool isBombActive = _bomb != null && (_bomb.bombState.Value == BombState.Planted || _bomb.bombState.Value == BombState.Defusing);

            if (isBombActive)
            {
                if (_bomb.plantedServerTime.Value > 0)
                {
                    double elapsedDetonation = NetworkManager.Singleton.ServerTime.Time - _bomb.plantedServerTime.Value;
                    int bombRemaining = Mathf.CeilToInt(Mathf.Max(_bomb.DetonationTimeDuration - (float)elapsedDetonation, 0f));

                    if (bombRemaining != lastSecondReported)
                    {
                        lastSecondReported = bombRemaining;
                        CurrentRoundTime.Value = bombRemaining;
                    }

                    if (elapsedDetonation >= _bomb.DetonationTimeDuration)
                    {
                        EndRoundServer(Team.Red);
                        break;
                    }
                }

                CheckEliminationsAfterPlant();
            }
            else
            {
                double elapsed = GetPhaseElapsed();
                int roundRemaining = Mathf.CeilToInt(Mathf.Max(_roundTimeLimit - (float)elapsed, 0f));

                if (roundRemaining != lastSecondReported)
                {
                    lastSecondReported = roundRemaining;
                    CurrentRoundTime.Value = roundRemaining;
                }

                if (elapsed >= _roundTimeLimit)
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
        CurrentPhase.Value = RoundPhase.RoundEnd;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;

        int lastSecondReported = -1;

        while (true)
        {
            double elapsed = GetPhaseElapsed();
            int remaining = Mathf.CeilToInt(Mathf.Max(_roundEndDisplayDuration - (float)elapsed, 0f));

            if (remaining != lastSecondReported)
            {
                lastSecondReported = remaining;
                CurrentRoundTime.Value = remaining;
            }

            if (elapsed >= _roundEndDisplayDuration) break;
            yield return null;
        }
    }

    private void CheckEliminationsBeforePlant()
    {
        int totalRed = 0, totalBlue = 0, aliveRed = 0, aliveBlue = 0;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkClient client = clientPair.Value;
            if (client.PlayerObject == null) continue;

            PlayerTeam teamComp = client.PlayerObject.GetComponent<PlayerTeam>();
            if (teamComp == null) teamComp = client.PlayerObject.GetComponentInChildren<PlayerTeam>();
            NetworkHealth healthComp = client.PlayerObject.GetComponent<NetworkHealth>();
            if (healthComp == null) healthComp = client.PlayerObject.GetComponentInChildren<NetworkHealth>();

            if (teamComp == null || healthComp == null) continue;

            if (teamComp.CurrentTeam.Value == Team.Red)
            {
                totalRed++;
                if (healthComp.IsAlive.Value && healthComp.CurrentHealth.Value > 0f) aliveRed++;
            }
            else if (teamComp.CurrentTeam.Value == Team.Blue)
            {
                totalBlue++;
                if (healthComp.IsAlive.Value && healthComp.CurrentHealth.Value > 0f) aliveBlue++;
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

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkClient client = clientPair.Value;
            if (client.PlayerObject == null) continue;

            PlayerTeam teamComp = client.PlayerObject.GetComponent<PlayerTeam>();
            if (teamComp == null) teamComp = client.PlayerObject.GetComponentInChildren<PlayerTeam>();
            NetworkHealth healthComp = client.PlayerObject.GetComponent<NetworkHealth>();
            if (healthComp == null) healthComp = client.PlayerObject.GetComponentInChildren<NetworkHealth>();

            if (teamComp == null || healthComp == null) continue;

            if (teamComp.CurrentTeam.Value == Team.Blue)
            {
                totalBlue++;
                if (healthComp.IsAlive.Value && healthComp.CurrentHealth.Value > 0f) aliveBlue++;
            }
        }

        if (totalBlue > 0 && aliveBlue <= 0) EndRoundServer(Team.Red);
    }

    private void EndRoundServer(Team winner)
    {
        if (CurrentPhase.Value != RoundPhase.InProgress) return;

        LastRoundWinner.Value = winner;
        if (winner == Team.Red) RedScore.Value++;
        else if (winner == Team.Blue) BlueScore.Value++;

        CurrentRoundTime.Value = 0;
        CurrentPhase.Value = RoundPhase.RoundEnd;
    }

    private void ResetEntitiesForNewRound()
    {
        if (NetworkManager.Singleton == null) return;

        int tIndex = 0;
        int ctIndex = 0;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;
            NetworkClient client = clientPair.Value;

            Team team = Team.Red;
            if (CustomLobbyManager.SavedTeams != null && CustomLobbyManager.SavedTeams.TryGetValue(clientId, out Team savedTeam))
            {
                team = savedTeam;
            }

            if (team == Team.Neutral && NetworkManager.Singleton.ConnectedClients.Count > 1)
            {
                // Mantiene el modo espectador
            }
            else if (team == Team.Neutral)
            {
                team = Team.Red;
            }

            Transform[] spawns = (team == Team.Red) ? _terroristSpawnPoints : _counterTerroristSpawnPoints;
            if (spawns == null || spawns.Length == 0) spawns = _terroristSpawnPoints;

            int spawnIndex = (team == Team.Red) ? tIndex++ : ctIndex++;
            Vector3 targetPosition = Vector3.up * 2f;
            Quaternion targetRotation = Quaternion.identity;

            if (spawns != null && spawns.Length > 0)
            {
                Transform targetSpawn = spawns[spawnIndex % spawns.Length];
                if (targetSpawn != null)
                {
                    targetPosition = targetSpawn.position;
                    targetRotation = targetSpawn.rotation;
                }
            }

            GameObject activePlayer = null;

            if (client.PlayerObject == null)
            {
                GameObject prefabToSpawn = _playerPrefab;
                if (prefabToSpawn == null && NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
                {
                    prefabToSpawn = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                }

                if (prefabToSpawn != null)
                {
                    activePlayer = Instantiate(prefabToSpawn, targetPosition, targetRotation);
                    NetworkObject netObj = activePlayer.GetComponent<NetworkObject>();
                    netObj.SpawnAsPlayerObject(clientId, true);
                }
            }
            else
            {
                activePlayer = client.PlayerObject.gameObject;
            }

            if (activePlayer != null)
            {
                NetworkHealth health = activePlayer.GetComponent<NetworkHealth>();
                if (health == null) health = activePlayer.GetComponentInChildren<NetworkHealth>();
                if (health != null) health.ResetHealthServer();

                ResetPlayerWeaponsClientRpc(clientId);

                PlayerTeam playerTeamComp = activePlayer.GetComponent<PlayerTeam>();
                if (playerTeamComp == null) playerTeamComp = activePlayer.GetComponentInChildren<PlayerTeam>();
                if (playerTeamComp != null) playerTeamComp.CurrentTeam.Value = team;

                if (CustomLobbyManager.SavedRoles != null && CustomLobbyManager.SavedRoles.TryGetValue(clientId, out PlayerRoleType savedRole))
                {
                    NetworkPlayerController controller = activePlayer.GetComponent<NetworkPlayerController>();
                    if (controller != null) controller.SetInitialRole(savedRole);
                }

                NetworkPlayerController playerController = activePlayer.GetComponent<NetworkPlayerController>();
                if (playerController != null)
                {
                    playerController.RespawnPlayer(targetPosition, targetRotation);
                }

                TeleportPlayerClientRpc(targetPosition, targetRotation, clientId);
            }
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

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetPlayerWeaponsClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;
        NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetClientId);
        if (playerObject == null) return;

        WeaponInventory inventory = playerObject.GetComponentInChildren<WeaponInventory>(true);
        if (inventory != null) inventory.ResetAllWeaponsAmmo();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TeleportPlayerClientRpc(Vector3 targetPosition, Quaternion targetRotation, ulong targetClientId)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;
        NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetClientId);
        if (playerObject == null) return;

        NetworkPlayerController playerController = playerObject.GetComponent<NetworkPlayerController>();
        if (playerController != null) playerController.RespawnPlayer(targetPosition, targetRotation);

        Unity.Netcode.Components.NetworkTransform networkTransform = playerObject.GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (networkTransform != null) networkTransform.Teleport(targetPosition, targetRotation, playerObject.transform.localScale);
    }

    private double GetPhaseElapsed()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.ServerTime.Time - PhaseStartServerTime.Value;
    }
}