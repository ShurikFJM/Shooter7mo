using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum RoundPhase : byte
{
    WaitingForPlayers,
    FreezeTime,
    Active,
    PostRound
}

[RequireComponent(typeof(NetworkObject))]
public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] private float _freezeTimeDuration = 7f;
    [SerializeField] private float _roundTimeDuration = 115f;
    [SerializeField] private float _postRoundDuration = 5f;
    [SerializeField] private Transform[] _terroristSpawns;
    [SerializeField] private Transform[] _counterTerroristSpawns;
    [SerializeField] private Transform _defaultBombSpawn;
    [SerializeField] private Bomb _matchBomb;

    public NetworkVariable<RoundPhase> CurrentPhase = new NetworkVariable<RoundPhase>(
        RoundPhase.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> PhaseTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> TerroristScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> CounterTerroristScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<TeamSide> WinningTeam = new NetworkVariable<TeamSide>(
        TeamSide.Terrorist,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly HashSet<ulong> _lockedInClients = new HashSet<ulong>();
    private Coroutine _roundLoopCoroutine;
    private bool _isBombPlanted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (_matchBomb == null)
            {
                _matchBomb = FindAnyObjectByType<Bomb>();
            }

            if (_matchBomb != null)
            {
                _matchBomb.State.OnValueChanged += HandleBombStateChanged;
            }

            CurrentPhase.Value = RoundPhase.WaitingForPlayers;
            _lockedInClients.Clear();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (_matchBomb != null)
            {
                _matchBomb.State.OnValueChanged -= HandleBombStateChanged;
            }

            if (_roundLoopCoroutine != null)
            {
                StopCoroutine(_roundLoopCoroutine);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NotifyPlayerLockedInServerRpc(ulong clientId)
    {
        if (CurrentPhase.Value != RoundPhase.WaitingForPlayers) return;

        _lockedInClients.Add(clientId);

        int totalConnectedClients = NetworkManager.Singleton.ConnectedClients.Count;
        if (_lockedInClients.Count >= totalConnectedClients && totalConnectedClients > 0)
        {
            StartMatchServer();
        }
    }

    public void StartMatchServer()
    {
        if (!IsServer) return;

        if (_roundLoopCoroutine != null)
        {
            StopCoroutine(_roundLoopCoroutine);
        }

        _roundLoopCoroutine = StartCoroutine(MatchLoopRoutine());
    }

    private void HandleBombStateChanged(BombState previousState, BombState currentState)
    {
        if (!IsServer || CurrentPhase.Value != RoundPhase.Active) return;

        if (currentState == BombState.Planted)
        {
            _isBombPlanted = true;
            PhaseTimer.Value = _matchBomb.DetonationTimeDuration;
        }
        else if (currentState == BombState.Defused)
        {
            EndRoundServer(TeamSide.CounterTerrorist);
        }
        else if (currentState == BombState.Exploded)
        {
            EndRoundServer(TeamSide.Terrorist);
        }
    }

    private IEnumerator MatchLoopRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(FreezeTimeRoutine());
            yield return StartCoroutine(ActiveRoundRoutine());
            yield return StartCoroutine(PostRoundRoutine());
        }
    }

    private IEnumerator FreezeTimeRoutine()
    {
        CurrentPhase.Value = RoundPhase.FreezeTime;
        PhaseTimer.Value = _freezeTimeDuration;
        _isBombPlanted = false;

        ResetRoundEntitiesServer();

        while (PhaseTimer.Value > 0f)
        {
            PhaseTimer.Value -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ActiveRoundRoutine()
    {
        CurrentPhase.Value = RoundPhase.Active;
        PhaseTimer.Value = _roundTimeDuration;

        while (CurrentPhase.Value == RoundPhase.Active)
        {
            PhaseTimer.Value -= Time.deltaTime;

            EvaluateTeamElimination();

            if (PhaseTimer.Value <= 0f)
            {
                if (_isBombPlanted)
                {
                    EndRoundServer(TeamSide.Terrorist);
                }
                else
                {
                    EndRoundServer(TeamSide.CounterTerrorist);
                }
            }

            yield return null;
        }
    }

    private IEnumerator PostRoundRoutine()
    {
        CurrentPhase.Value = RoundPhase.PostRound;
        PhaseTimer.Value = _postRoundDuration;

        while (PhaseTimer.Value > 0f)
        {
            PhaseTimer.Value -= Time.deltaTime;
            yield return null;
        }
    }

    private void EvaluateTeamElimination()
    {
        int aliveTerrorists = 0;
        int aliveCounterTerrorists = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            NetworkHealth health = client.PlayerObject.GetComponent<NetworkHealth>();
            TacticalChatManager chat = client.PlayerObject.GetComponent<TacticalChatManager>();

            if (health == null || health.CurrentHealth.Value <= 0) continue;

            TeamSide team = chat != null ? chat.localTeam : TeamSide.Terrorist;
            if (team == TeamSide.Terrorist) aliveTerrorists++;
            else aliveCounterTerrorists++;
        }

        if (aliveTerrorists == 0 && !_isBombPlanted)
        {
            EndRoundServer(TeamSide.CounterTerrorist);
        }
        else if (aliveCounterTerrorists == 0)
        {
            EndRoundServer(TeamSide.Terrorist);
        }
    }

    private void EndRoundServer(TeamSide winner)
    {
        if (CurrentPhase.Value != RoundPhase.Active) return;

        WinningTeam.Value = winner;
        if (winner == TeamSide.Terrorist) TerroristScore.Value++;
        else CounterTerroristScore.Value++;

        CurrentPhase.Value = RoundPhase.PostRound;
    }

    private void ResetRoundEntitiesServer()
    {
        int tIndex = 0;
        int ctIndex = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            NetworkHealth health = client.PlayerObject.GetComponent<NetworkHealth>();
            if (health != null)
            {
                health.ResetHealthServer();
            }

            TacticalChatManager chat = client.PlayerObject.GetComponent<TacticalChatManager>();
            TeamSide team = chat != null ? chat.localTeam : TeamSide.Terrorist;

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (team == TeamSide.Terrorist && _terroristSpawns != null && _terroristSpawns.Length > 0)
            {
                Transform point = _terroristSpawns[tIndex % _terroristSpawns.Length];
                spawnPosition = point.position;
                spawnRotation = point.rotation;
                tIndex++;
            }
            else if (team == TeamSide.CounterTerrorist && _counterTerroristSpawns != null && _counterTerroristSpawns.Length > 0)
            {
                Transform point = _counterTerroristSpawns[ctIndex % _counterTerroristSpawns.Length];
                spawnPosition = point.position;
                spawnRotation = point.rotation;
                ctIndex++;
            }

            TeleportPlayerClientRpc(spawnPosition, spawnRotation, client.ClientId);
        }

        if (_matchBomb != null)
        {
            Vector3 bombSpawnPos = _defaultBombSpawn != null ? _defaultBombSpawn.position : Vector3.zero;
            Quaternion bombSpawnRot = _defaultBombSpawn != null ? _defaultBombSpawn.rotation : Quaternion.identity;

            _matchBomb.transform.SetPositionAndRotation(bombSpawnPos, bombSpawnRot);
            _matchBomb.State.Value = BombState.Dropped;
            _matchBomb.CarrierClientId.Value = ulong.MaxValue;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TeleportPlayerClientRpc(Vector3 targetPosition, Quaternion targetRotation, ulong targetClientId)
    {
        if (NetworkManager.Singleton.SpawnManager == null) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out NetworkClient client)) return;
        if (client.PlayerObject == null) return;

        GameObject playerObj = client.PlayerObject.gameObject;
        CharacterController cc = playerObj.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;
        playerObj.transform.SetPositionAndRotation(targetPosition, targetRotation);
        if (cc != null) cc.enabled = true;
    }
}