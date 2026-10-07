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
    public static RoundManager Instance { get; private set; }

    [SerializeField] private Bomb _bomb;
    [SerializeField] private Transform[] _terroristSpawnPoints;
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;
    [SerializeField] private Transform _defaultBombSpawn;
    [SerializeField] private float _warmupDuration = 7f;
    [SerializeField] private float _roundTimeLimit = 115f;
    [SerializeField] private float _roundEndDisplayDuration = 5f;
    [SerializeField] private int _roundsToWinMatch = 13;

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

    public NetworkVariable<int> TerroristScore => RedScore;
    public NetworkVariable<int> CounterTerroristScore => BlueScore;

    private readonly HashSet<ulong> _lockedInClients = new HashSet<ulong>();
    private Coroutine _roundLoopCoroutine;

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
            _bomb = FindAnyObjectByType<Bomb>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (_bomb == null)
            {
                _bomb = FindAnyObjectByType<Bomb>();
            }

            if (_bomb != null)
            {
                _bomb.State.OnValueChanged += HandleBombStateChanged;
            }

            CurrentPhase.Value = RoundPhase.WaitingForPlayers;
            _lockedInClients.Clear();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (_bomb != null)
            {
                _bomb.State.OnValueChanged -= HandleBombStateChanged;
            }

            if (_roundLoopCoroutine != null)
            {
                StopCoroutine(_roundLoopCoroutine);
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NotifyPlayerLockedInServerRpc(ulong clientId)
    {
        if (CurrentPhase.Value != RoundPhase.WaitingForPlayers) return;

        _lockedInClients.Add(clientId);

        int totalClients = NetworkManager.Singleton.ConnectedClients.Count;
        if (_lockedInClients.Count >= totalClients && totalClients > 0)
        {
            StartMatchServer();
        }
    }

    public void StartMatchServer()
    {
        if (!IsServer) return;

        RedScore.Value = 0;
        BlueScore.Value = 0;
        RoundNumber.Value = 0;
        LastRoundWinner.Value = Team.Neutral;

        if (_roundLoopCoroutine != null)
        {
            StopCoroutine(_roundLoopCoroutine);
        }

        _roundLoopCoroutine = StartCoroutine(MatchLoopRoutine());
    }

    private void HandleBombStateChanged(BombState previousState, BombState currentState)
    {
        if (!IsServer || CurrentPhase.Value != RoundPhase.InProgress) return;

        if (currentState == BombState.Planted)
        {
            IsBombPlanted.Value = true;
        }
        else if (currentState == BombState.Defused)
        {
            EndRoundServer(Team.Blue);
        }
        else if (currentState == BombState.Exploded)
        {
            EndRoundServer(Team.Red);
        }
    }

    private IEnumerator MatchLoopRoutine()
    {
        while (RedScore.Value < _roundsToWinMatch && BlueScore.Value < _roundsToWinMatch)
        {
            RoundNumber.Value++;
            yield return StartCoroutine(WarmupRoutine());
            yield return StartCoroutine(InProgressRoutine());
            yield return StartCoroutine(RoundEndRoutine());
        }

        CurrentPhase.Value = RoundPhase.MatchEnd;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    private IEnumerator WarmupRoutine()
    {
        CurrentPhase.Value = RoundPhase.Warmup;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        CurrentRoundTime.Value = Mathf.CeilToInt(_warmupDuration);
        IsBombPlanted.Value = false;

        ResetEntitiesForNewRound();

        while (true)
        {
            double elapsed = GetPhaseElapsed();
            int remaining = Mathf.CeilToInt(Mathf.Max(_warmupDuration - (float)elapsed, 0f));
            CurrentRoundTime.Value = remaining;

            if (elapsed >= _warmupDuration) break;
            yield return null;
        }
    }

    private IEnumerator InProgressRoutine()
    {
        CurrentPhase.Value = RoundPhase.InProgress;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        CurrentRoundTime.Value = Mathf.CeilToInt(_roundTimeLimit);

        while (CurrentPhase.Value == RoundPhase.InProgress)
        {
            if (_bomb != null && _bomb.State.Value == BombState.Planted)
            {
                if (_bomb.PlantedServerTime.Value > 0)
                {
                    double elapsedDetonation = NetworkManager.Singleton.ServerTime.Time - _bomb.PlantedServerTime.Value;
                    int bombRemaining = Mathf.CeilToInt(Mathf.Max(_bomb.DetonationTimeDuration - (float)elapsedDetonation, 0f));
                    CurrentRoundTime.Value = bombRemaining;
                }
            }
            else
            {
                double elapsed = GetPhaseElapsed();
                int roundRemaining = Mathf.CeilToInt(Mathf.Max(_roundTimeLimit - (float)elapsed, 0f));
                CurrentRoundTime.Value = roundRemaining;

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

        while (true)
        {
            double elapsed = GetPhaseElapsed();
            int remaining = Mathf.CeilToInt(Mathf.Max(_roundEndDisplayDuration - (float)elapsed, 0f));
            CurrentRoundTime.Value = remaining;

            if (elapsed >= _roundEndDisplayDuration) break;
            yield return null;
        }
    }

    private void CheckEliminationsBeforePlant()
    {
        int aliveTerrorists = CountAlivePlayers(Team.Red);
        int aliveCounterTerrorists = CountAlivePlayers(Team.Blue);

        if (aliveTerrorists <= 0 && aliveCounterTerrorists > 0)
        {
            EndRoundServer(Team.Blue);
        }
        else if (aliveCounterTerrorists <= 0 && aliveTerrorists > 0)
        {
            EndRoundServer(Team.Red);
        }
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
        PlayerTeam[] allPlayers = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude);
        int tIndex = 0;
        int ctIndex = 0;

        foreach (PlayerTeam player in allPlayers)
        {
            if (player == null) continue;

            Team team = player.CurrentTeam.Value;
            Transform[] spawns = (team == Team.Red) ? _terroristSpawnPoints : _counterTerroristSpawnPoints;
            int spawnIndex = (team == Team.Red) ? tIndex++ : ctIndex++;

            if (spawns != null && spawns.Length > 0)
            {
                Transform targetSpawn = spawns[spawnIndex % spawns.Length];
                TeleportPlayerClientRpc(targetSpawn.position, targetSpawn.rotation, player.OwnerClientId);
            }

            NetworkHealth health = player.GetComponent<NetworkHealth>();
            if (health != null)
            {
                health.ResetHealthServer();
            }
        }

        if (_bomb != null)
        {
            Vector3 bombPosition = (_defaultBombSpawn != null)
                ? _defaultBombSpawn.position
                : (_terroristSpawnPoints != null && _terroristSpawnPoints.Length > 0 ? _terroristSpawnPoints[0].position + Vector3.up * 0.3f : Vector3.up * 0.3f);

            _bomb.ServerResetBomb(bombPosition);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TeleportPlayerClientRpc(Vector3 targetPosition, Quaternion targetRotation, ulong targetClientId)
    {
        if (NetworkManager.Singleton.SpawnManager == null) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out NetworkClient client)) return;
        if (client.PlayerObject == null) return;

        GameObject playerObj = client.PlayerObject.gameObject;
        CharacterController characterController = playerObj.GetComponent<CharacterController>();

        if (characterController != null) characterController.enabled = false;
        playerObj.transform.SetPositionAndRotation(targetPosition, targetRotation);
        if (characterController != null) characterController.enabled = true;
    }

    private int CountAlivePlayers(Team team)
    {
        PlayerTeam[] allPlayers = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude);
        int count = 0;

        foreach (PlayerTeam player in allPlayers)
        {
            if (player == null || player.CurrentTeam.Value != team) continue;

            NetworkHealth health = player.GetComponent<NetworkHealth>();
            if (health != null && health.CurrentHealth.Value > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private double GetPhaseElapsed()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.ServerTime.Time - PhaseStartServerTime.Value;
    }
}