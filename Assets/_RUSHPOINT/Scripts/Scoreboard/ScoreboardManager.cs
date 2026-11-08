using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerScoreStats : INetworkSerializable, IEquatable<PlayerScoreStats>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public Team PlayerTeam;
    public int Kills;
    public int Deaths;
    public int Assists;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref PlayerTeam);
        serializer.SerializeValue(ref Kills);
        serializer.SerializeValue(ref Deaths);
        serializer.SerializeValue(ref Assists);
    }

    public bool Equals(PlayerScoreStats other)
    {
        return ClientId == other.ClientId && PlayerName == other.PlayerName &&
               PlayerTeam == other.PlayerTeam && Kills == other.Kills &&
               Deaths == other.Deaths && Assists == other.Assists;
    }
}

public class ScoreboardManager : NetworkBehaviour
{
    public static ScoreboardManager Instance { get; private set; }
    public NetworkList<PlayerScoreStats> PlayerStats = new NetworkList<PlayerScoreStats>();

    public event Action OnStatsReady;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        int copies = FindObjectsByType<ScoreboardManager>().Length;
        Debug.Log($"[Scoreboard] OnNetworkSpawn | objId={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)} | " +
        $"IsServer={IsServer} | IsSpawned={IsSpawned} | ScoreboardManagers en escena={copies}");
        if (copies > 1)
        {
            Debug.LogWarning("[Scoreboard] Hay más de un ScoreboardManager en la escena. Borra el que sobra.");
        }

        if (IsServer)
        {
            StartCoroutine(PopulateRoutine());
        }

        OnStatsReady?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();
        if (Instance == this) Instance = null;
        base.OnNetworkDespawn();
    }

    private IEnumerator PopulateRoutine()
    {
        yield return null;
        FillFromLobbyAndPlayers();
        var wait = new WaitForSecondsRealtime(0.25f);
        for (int attempt = 1; attempt <= 4; attempt++)
        {
            yield return wait;
            if (!IsSpawned || !IsServer) yield break;

            int missing = CountMissingFromCache();
            if (missing > 0)
            {
                Debug.LogWarning($"[Scoreboard] Faltan {missing} jugadores del lobby en la lista (intento {attempt}). Se rellena otra vez.");
                FillFromLobbyAndPlayers();
            }
        }
    }

    private int CountMissingFromCache()
    {
        int missing = 0;
        if (MatchDataBridge.PlayersCache == null) return 0;
        foreach (var kvp in MatchDataBridge.PlayersCache)
        {
            if (FindIndex(kvp.Key) < 0) missing++;
        }
        return missing;
    }

    private void FillFromLobbyAndPlayers()
    {
        if (!IsServer || !IsSpawned) return;

        var cache = MatchDataBridge.PlayersCache;
        if (cache == null || cache.Count == 0)
        {
            Debug.LogError("❌ [Scoreboard] MatchDataBridge está VACÍO. El lobby no guardó los datos antes de cargar la escena.");
        }
        else
        {
            foreach (var kvp in cache)
            {
                Debug.Log($"[Scoreboard] Lobby: '{kvp.Value.Name}' (id {kvp.Key}) -> equipo {kvp.Value.SelectedTeam}");
                RegisterPlayerServer(kvp.Key, kvp.Value.Name, kvp.Value.SelectedTeam, updateExisting: false);
            }
        }

        if (NetworkManager != null)
        {
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (FindIndex(client.ClientId) >= 0) continue;

                Team team = Team.Neutral;
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerTeam playerTeam))
                {
                    team = playerTeam.CurrentTeam.Value;
                }
                RegisterPlayerServer(client.ClientId, $"Player_{client.ClientId}", team, updateExisting: false);
            }
        }

        Debug.Log($"✅ [Scoreboard] Lista: {PlayerStats.Count} jugadores | IsSpawned={IsSpawned} | " +
                  $"objId={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
    }

    public void RegisterPlayerServer(ulong clientId, string playerName, Team team, bool updateExisting = true)
    {
        if (!IsServer || !IsSpawned) return;

        FixedString32Bytes fixedName = ToFixedName(playerName);
        int index = FindIndex(clientId);

        if (index < 0)
        {
            PlayerStats.Add(new PlayerScoreStats
            {
                ClientId = clientId,
                PlayerName = fixedName,
                PlayerTeam = team,
                Kills = 0,
                Deaths = 0,
                Assists = 0
            });
            return;
        }

        if (!updateExisting) return;

        var stat = PlayerStats[index];
        if (stat.PlayerTeam != team || stat.PlayerName != fixedName)
        {
            stat.PlayerTeam = team;
            stat.PlayerName = fixedName;
            PlayerStats[index] = stat;
        }
    }

    public void UpdatePlayerTeamServer(ulong clientId, Team newTeam)
    {
        if (!IsServer) return;

        int index = FindIndex(clientId);
        if (index < 0)
        {
            RegisterPlayerServer(clientId, $"Player_{clientId}", newTeam);
            return;
        }

        var stat = PlayerStats[index];
        stat.PlayerTeam = newTeam;
        PlayerStats[index] = stat;
    }

    public void RegisterKillAndDeathServer(ulong killerId, ulong victimId)
    {
        if (!IsServer) return;

        if (killerId != victimId)
        {
            int killerIndex = FindIndex(killerId);
            if (killerIndex >= 0)
            {
                var killer = PlayerStats[killerIndex];
                killer.Kills++;
                PlayerStats[killerIndex] = killer;
            }
        }

        int victimIndex = FindIndex(victimId);
        if (victimIndex >= 0)
        {
            var victim = PlayerStats[victimIndex];
            victim.Deaths++;
            PlayerStats[victimIndex] = victim;
        }
    }

    public void RegisterAssistServer(ulong assistingPlayerId)
    {
        if (!IsServer) return;

        int index = FindIndex(assistingPlayerId);
        if (index < 0) return;

        var stat = PlayerStats[index];
        stat.Assists++;
        PlayerStats[index] = stat;
    }

    private int FindIndex(ulong clientId)
    {
        for (int i = 0; i < PlayerStats.Count; i++)
        {
            if (PlayerStats[i].ClientId == clientId) return i;
        }
        return -1;
    }
    private static FixedString32Bytes ToFixedName(string name)
    {
        var fixedName = new FixedString32Bytes();
        fixedName.CopyFromTruncated(string.IsNullOrEmpty(name) ? "Player" : name);
        return fixedName;
    }
}