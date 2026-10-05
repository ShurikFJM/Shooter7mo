using Unity.Netcode;
using UnityEngine;

public enum RoundPhase : byte
{
    Warmup,
    InProgress,
    RoundEnd,
    MatchEnd
}

/// <summary>
/// Controla el flujo de rondas estilo CS2 (Search & Destroy): warmup, inicio
/// de ronda, condiciones de victoria, marcador y reinicio. Autoridad total en
/// el servidor; los clientes solo leen las NetworkVariables para su UI.
///
/// Se suscribe a Bomb.OnTerroristsWin / Bomb.OnCounterTerroristsWin para dos
/// de las cuatro condiciones de victoria; las otras dos (terroristas
/// eliminados, tiempo agotado sin plantar) se evalúan aquí mismo cada frame
/// mientras la fase es InProgress.
///
/// IMPORTANTE: CountAlivePlayers() tiene un TODO sin resolver — no conozco el
/// API real de tu NetworkHealth (ese script no se compartió), así que por
/// ahora cuenta a todos los jugadores del equipo como "vivos" sin excepción.
/// La condición de "Terroristas eliminados" NO funcionará correctamente hasta
/// que conectes ese chequeo.
/// </summary>
public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] private Bomb _bomb;
    [SerializeField] private Transform[] _terroristSpawnPoints;
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;
    [SerializeField] private float _warmupDuration = 5f;
    [SerializeField] private float _roundTimeLimit = 115f;
    [SerializeField] private float _roundEndDisplayDuration = 5f;
    [SerializeField] private int _roundsToWinMatch = 13;

    public NetworkVariable<RoundPhase> CurrentPhase = new NetworkVariable<RoundPhase>(
        RoundPhase.Warmup, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> PhaseStartServerTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RedScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> BlueScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RoundNumber = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Team> LastRoundWinner = new NetworkVariable<Team>(
        Team.Neutral, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float RoundTimeLimit => _roundTimeLimit;
    public float WarmupDuration => _warmupDuration;
    public float RoundEndDisplayDuration => _roundEndDisplayDuration;

    private void Awake()
    {
        Instance = this;

        if (_bomb == null)
        {
            _bomb = FindFirstObjectByType<Bomb>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (_bomb != null)
        {
            _bomb.OnTerroristsWin += HandleTerroristsWin;
            _bomb.OnCounterTerroristsWin += HandleCounterTerroristsWin;
        }

        BeginWarmup();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (_bomb != null)
        {
            _bomb.OnTerroristsWin -= HandleTerroristsWin;
            _bomb.OnCounterTerroristsWin -= HandleCounterTerroristsWin;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        switch (CurrentPhase.Value)
        {
            case RoundPhase.Warmup:
                if (GetPhaseElapsed() >= _warmupDuration)
                {
                    BeginRoundInProgress();
                }
                break;

            case RoundPhase.InProgress:
                EvaluateInProgressWinConditions();
                break;

            case RoundPhase.RoundEnd:
                if (GetPhaseElapsed() >= _roundEndDisplayDuration)
                {
                    BeginWarmup();
                }
                break;

            case RoundPhase.MatchEnd:
                // Se queda aquí hasta que llames StartNewMatch() manualmente
                // (ej. desde un botón de "Jugar de nuevo" en tu UI de fin de partida).
                break;
        }
    }

    private double GetPhaseElapsed()
    {
        return NetworkManager.Singleton.ServerTime.Time - PhaseStartServerTime.Value;
    }

    private void EvaluateInProgressWinConditions()
    {
        if (_bomb == null) return;

        bool bombPlanted = _bomb.State.Value == BombState.Planted;

        // Terroristas eliminados y la bomba nunca se plantó: ganan los CT.
        if (!bombPlanted && CountAlivePlayers(Team.Red) <= 0)
        {
            EndRound(Team.Blue);
            return;
        }

        // Se acabó el tiempo sin plantar: ganan los CT.
        // (Si ya está plantada, el reloj que manda es el de detonación de Bomb.cs,
        // no este límite de ronda — por eso se ignora el tiempo una vez plantada.)
        if (!bombPlanted && GetPhaseElapsed() >= _roundTimeLimit)
        {
            EndRound(Team.Blue);
        }
    }

    private void HandleTerroristsWin()
    {
        if (CurrentPhase.Value != RoundPhase.InProgress) return;
        EndRound(Team.Red);
    }

    private void HandleCounterTerroristsWin()
    {
        if (CurrentPhase.Value != RoundPhase.InProgress) return;
        EndRound(Team.Blue);
    }

    private void EndRound(Team winner)
    {
        LastRoundWinner.Value = winner;

        if (winner == Team.Red) RedScore.Value++;
        else if (winner == Team.Blue) BlueScore.Value++;

        if (RedScore.Value >= _roundsToWinMatch || BlueScore.Value >= _roundsToWinMatch)
        {
            CurrentPhase.Value = RoundPhase.MatchEnd;
            PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
            return;
        }

        CurrentPhase.Value = RoundPhase.RoundEnd;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    private void BeginWarmup()
    {
        RoundNumber.Value++;
        ResetBombForNewRound();
        RespawnAndResetPlayers();

        CurrentPhase.Value = RoundPhase.Warmup;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    private void BeginRoundInProgress()
    {
        CurrentPhase.Value = RoundPhase.InProgress;
        PhaseStartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    /// <summary>Llamar manualmente (ej. desde un botón de "Jugar de nuevo") para arrancar otra partida desde cero.</summary>
    public void StartNewMatch()
    {
        if (!IsServer) return;

        RedScore.Value = 0;
        BlueScore.Value = 0;
        RoundNumber.Value = 0;
        BeginWarmup();
    }

    private void ResetBombForNewRound()
    {
        if (_bomb == null) return;

        Vector3 spawnPosition = _terroristSpawnPoints != null && _terroristSpawnPoints.Length > 0
            ? _terroristSpawnPoints[0].position
            : _bomb.transform.position;

        _bomb.ServerResetBomb(spawnPosition);
    }

    private void RespawnAndResetPlayers()
    {
        PlayerTeam[] allPlayers = FindObjectsByType<PlayerTeam>(FindObjectsSortMode.None);

        foreach (PlayerTeam player in allPlayers)
        {
            Transform[] spawnPoints = player.CurrentTeam.Value == Team.Red ? _terroristSpawnPoints : _counterTerroristSpawnPoints;
            if (spawnPoints == null || spawnPoints.Length == 0) continue;

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            TeleportPlayer(player.gameObject, spawnPoint.position, spawnPoint.rotation);

            // TODO: resetear vida/armadura aquí con el API real de tu NetworkHealth, ej.:
            // NetworkHealth health = player.GetComponent<NetworkHealth>();
            // health?.ResetServer(); // ajusta el nombre del método al que exista en tu script
        }
    }

    private void TeleportPlayer(GameObject playerObject, Vector3 position, Quaternion rotation)
    {
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;

        playerObject.transform.SetPositionAndRotation(position, rotation);

        if (characterController != null) characterController.enabled = true;
    }

    private int CountAlivePlayers(Team team)
    {
        PlayerTeam[] allPlayers = FindObjectsByType<PlayerTeam>(FindObjectsSortMode.None);
        int count = 0;

        foreach (PlayerTeam player in allPlayers)
        {
            if (player.CurrentTeam.Value != team) continue;

            // TODO: ajusta este chequeo al API real de tu NetworkHealth (no se compartió ese script).
            // Ejemplo esperado:
            // NetworkHealth health = player.GetComponent<NetworkHealth>();
            // if (health != null && !health.IsAlive) continue;

            count++;
        }

        return count;
    }
}
