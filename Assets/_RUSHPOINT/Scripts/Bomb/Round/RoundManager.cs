using Unity.Netcode;
using UnityEngine;

public enum RoundPhase : byte
{
    Warmup,
    InProgress,
    RoundEnd,
    MatchEnd
}

[RequireComponent(typeof(NetworkObject))]
public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Bomb _bomb;

    [Header("Terrorist Spawns")]
    [SerializeField] private Transform[] _terroristSpawnPoints;

    [Header("Counter-Terrorist Spawns")]
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;

    [Header("Round Settings")]
    [SerializeField] private float _warmupDuration = 5f;
    [SerializeField] private float _roundTimeLimit = 115f;
    [SerializeField] private float _roundEndDisplayDuration = 5f;
    [SerializeField] private int _roundsToWinMatch = 13;

    public NetworkVariable<RoundPhase> CurrentPhase =
        new NetworkVariable<RoundPhase>(
            RoundPhase.Warmup,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<double> PhaseStartServerTime =
        new NetworkVariable<double>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> CurrentRoundTime =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> RedScore =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> BlueScore =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> RoundNumber =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<Team> LastRoundWinner =
        new NetworkVariable<Team>(
            Team.Neutral,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<bool> IsBombPlanted =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public float RoundTimeLimit => _roundTimeLimit;
    public float WarmupDuration => _warmupDuration;
    public float RoundEndDisplayDuration => _roundEndDisplayDuration;
    public int RoundsToWinMatch => _roundsToWinMatch;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_bomb == null)
        {
            _bomb = FindFirstObjectByType<Bomb>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        if (_bomb == null)
        {
            _bomb = FindFirstObjectByType<Bomb>();
        }

        if (_bomb != null)
        {
            _bomb.OnTerroristsWin += HandleTerroristsWin;
            _bomb.OnCounterTerroristsWin += HandleCounterTerroristsWin;
        }

        StartNewRound();
    }

    public override void OnNetworkDespawn()
    {
        if (_bomb != null)
        {
            _bomb.OnTerroristsWin -= HandleTerroristsWin;
            _bomb.OnCounterTerroristsWin -= HandleCounterTerroristsWin;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        switch (CurrentPhase.Value)
        {
            case RoundPhase.Warmup:
                UpdateWarmup();
                break;

            case RoundPhase.InProgress:
                UpdateRound();
                break;

            case RoundPhase.RoundEnd:
                UpdateRoundEnd();
                break;

            case RoundPhase.MatchEnd:
                break;
        }
    }

    private void UpdateWarmup()
    {
        double elapsedTime = GetPhaseElapsed();

        int remainingTime = Mathf.CeilToInt(
            Mathf.Max(
                _warmupDuration - (float)elapsedTime,
                0f
            )
        );

        CurrentRoundTime.Value = remainingTime;

        if (elapsedTime >= _warmupDuration)
        {
            StartRound();
        }
    }

    private void StartRound()
    {
        CurrentPhase.Value = RoundPhase.InProgress;

        PhaseStartServerTime.Value =
            NetworkManager.Singleton.ServerTime.Time;

        CurrentRoundTime.Value =
            Mathf.CeilToInt(_roundTimeLimit);

        IsBombPlanted.Value = false;
    }

    private void UpdateRound()
    {
        if (_bomb == null)
        {
            return;
        }

        bool bombPlanted =
            _bomb.State.Value == BombState.Planted;
        if (bombPlanted)
        {
            UpdateBombTimer();
            return;
        }

        if (IsBombPlanted.Value)
        {
            IsBombPlanted.Value = false;
        }

        UpdateNormalRoundTimer();
        CheckPlayerEliminationBeforePlant();
    }

    private void UpdateNormalRoundTimer()
    {
        double elapsedTime = GetPhaseElapsed();

        int remainingTime = Mathf.CeilToInt(
            Mathf.Max(
                _roundTimeLimit - (float)elapsedTime,
                0f
            )
        );

        CurrentRoundTime.Value = remainingTime;
        if (elapsedTime >= _roundTimeLimit)
        {
            EndRound(Team.Blue);
        }
    }

    private void UpdateBombTimer()
    {
        if (!IsBombPlanted.Value)
        {
            IsBombPlanted.Value = true;
        }

        if (_bomb.PlantedServerTime.Value < 0)
        {
            return;
        }

        double elapsedTime =
            NetworkManager.Singleton.ServerTime.Time -
            _bomb.PlantedServerTime.Value;

        int remainingTime = Mathf.CeilToInt(
            Mathf.Max(
                _bomb.DetonationTimeDuration -
                (float)elapsedTime,
                0f
            )
        );

        CurrentRoundTime.Value = remainingTime;
    }

    private void CheckPlayerEliminationBeforePlant()
    {
        int terroristCount =
            CountAlivePlayers(Team.Red);

        int counterTerroristCount =
            CountAlivePlayers(Team.Blue);
        if (terroristCount <= 0 &&
            counterTerroristCount > 0)
        {
            EndRound(Team.Blue);
            return;
        }
        if (counterTerroristCount <= 0 &&
            terroristCount > 0)
        {
            EndRound(Team.Red);
        }
    }

    private void HandleTerroristsWin()
    {
        if (CurrentPhase.Value != RoundPhase.InProgress)
        {
            return;
        }

        EndRound(Team.Red);
    }

    private void HandleCounterTerroristsWin()
    {
        if (CurrentPhase.Value != RoundPhase.InProgress)
        {
            return;
        }
        EndRound(Team.Blue);
    }

    private void EndRound(Team winner)
    {
        if (CurrentPhase.Value != RoundPhase.InProgress)
        {
            return;
        }

        LastRoundWinner.Value = winner;

        if (winner == Team.Red)
        {
            RedScore.Value++;
        }
        else if (winner == Team.Blue)
        {
            BlueScore.Value++;
        }

        CurrentRoundTime.Value = 0;

        if (RedScore.Value >= _roundsToWinMatch ||
            BlueScore.Value >= _roundsToWinMatch)
        {
            CurrentPhase.Value = RoundPhase.MatchEnd;

            PhaseStartServerTime.Value =
                NetworkManager.Singleton.ServerTime.Time;

            return;
        }

        CurrentPhase.Value = RoundPhase.RoundEnd;

        PhaseStartServerTime.Value =
            NetworkManager.Singleton.ServerTime.Time;
    }

    private void UpdateRoundEnd()
    {
        double elapsedTime = GetPhaseElapsed();

        int remainingTime = Mathf.CeilToInt(
            Mathf.Max(
                _roundEndDisplayDuration -
                (float)elapsedTime,
                0f
            )
        );

        CurrentRoundTime.Value = remainingTime;

        if (elapsedTime >= _roundEndDisplayDuration)
        {
            StartNewRound();
        }
    }

    private void StartNewRound()
    {
        RoundNumber.Value++;

        ResetBombForNewRound();
        RespawnAndResetPlayers();

        IsBombPlanted.Value = false;

        CurrentPhase.Value = RoundPhase.Warmup;

        PhaseStartServerTime.Value =
            NetworkManager.Singleton.ServerTime.Time;

        CurrentRoundTime.Value =
            Mathf.CeilToInt(_warmupDuration);
    }

    public void StartNewMatch()
    {
        if (!IsServer)
        {
            return;
        }

        RedScore.Value = 0;
        BlueScore.Value = 0;
        RoundNumber.Value = 0;
        LastRoundWinner.Value = Team.Neutral;

        StartNewRound();
    }

    private void ResetBombForNewRound()
    {
        if (_bomb == null)
        {
            return;
        }

        Vector3 spawnPosition =
            _bomb.transform.position;

        if (_terroristSpawnPoints != null &&
            _terroristSpawnPoints.Length > 0)
        {
            spawnPosition =
                _terroristSpawnPoints[0].position;
        }

        _bomb.ServerResetBomb(spawnPosition);
    }

    private void RespawnAndResetPlayers()
    {
        PlayerTeam[] allPlayers =
            FindObjectsByType<PlayerTeam>(
                FindObjectsSortMode.None
            );

        int terroristSpawnIndex = 0;
        int counterTerroristSpawnIndex = 0;

        foreach (PlayerTeam player in allPlayers)
        {
            if (player == null)
            {
                continue;
            }

            Team team =
                player.CurrentTeam.Value;

            Transform[] spawnPoints = null;
            int spawnIndex = 0;

            if (team == Team.Red)
            {
                spawnPoints = _terroristSpawnPoints;
                spawnIndex = terroristSpawnIndex;
            }
            else if (team == Team.Blue)
            {
                spawnPoints = _counterTerroristSpawnPoints;
                spawnIndex = counterTerroristSpawnIndex;
            }

            if (spawnPoints == null ||
                spawnPoints.Length == 0)
            {
                continue;
            }

            Transform spawnPoint =
                spawnPoints[
                    spawnIndex % spawnPoints.Length
                ];

            TeleportPlayer(
                player.gameObject,
                spawnPoint.position,
                spawnPoint.rotation
            );

            NetworkHealth health =
                player.GetComponent<NetworkHealth>();

            if (health != null)
            {
                health.ResetHealthServer();
            }

            if (team == Team.Red)
            {
                terroristSpawnIndex++;
            }
            else if (team == Team.Blue)
            {
                counterTerroristSpawnIndex++;
            }
        }
    }

    private void TeleportPlayer(
        GameObject playerObject,
        Vector3 position,
        Quaternion rotation)
    {
        CharacterController characterController =
            playerObject.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Rigidbody rigidbody =
            playerObject.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        playerObject.transform.SetPositionAndRotation(
            position,
            rotation
        );

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private int CountAlivePlayers(Team team)
    {
        PlayerTeam[] allPlayers =
            FindObjectsByType<PlayerTeam>(
                FindObjectsSortMode.None
            );

        int alivePlayers = 0;

        foreach (PlayerTeam player in allPlayers)
        {
            if (player == null)
            {
                continue;
            }

            if (player.CurrentTeam.Value != team)
            {
                continue;
            }

            NetworkHealth health =
                player.GetComponent<NetworkHealth>();

            if (health == null)
            {
                continue;
            }

            if (health.IsAlive.Value &&
                health.CurrentHealth.Value > 0f)
            {
                alivePlayers++;
            }
        }

        return alivePlayers;
    }

    private double GetPhaseElapsed()
    {
        if (NetworkManager.Singleton == null)
        {
            return 0;
        }

        return NetworkManager.Singleton.ServerTime.Time -
               PhaseStartServerTime.Value;
    }
}