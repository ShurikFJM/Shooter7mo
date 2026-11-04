using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class KillFeedManager : NetworkBehaviour
{
    public static KillFeedManager Instance { get; private set; }

    [SerializeField] private KillFeedEntry _killFeedPrefab;
    [SerializeField] private Transform _killFeedContainer;
    [SerializeField] private Color _redTeamColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color _blueTeamColor = new Color(0.2f, 0.6f, 0.8f);
    [SerializeField] private Color _neutralColor = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void BroadcastKillServer(ulong killerId, ulong victimId)
    {
        if (!IsServer) return;

        string killerName = "Player";
        Team killerTeam = Team.Neutral;
        string victimName = "Player";
        Team victimTeam = Team.Neutral;

        if (ScoreboardManager.Instance != null)
        {
            foreach (var stat in ScoreboardManager.Instance.PlayerStats)
            {
                if (stat.ClientId == killerId)
                {
                    killerName = stat.PlayerName.ToString();
                    killerTeam = stat.PlayerTeam;
                }
                if (stat.ClientId == victimId)
                {
                    victimName = stat.PlayerName.ToString();
                    victimTeam = stat.PlayerTeam;
                }
            }
        }

        BroadcastKillClientRpc(
            new FixedString32Bytes(killerName),
            new FixedString32Bytes(victimName),
            killerTeam,
            victimTeam
        );
    }

    [ClientRpc]
    private void BroadcastKillClientRpc(FixedString32Bytes killerName, FixedString32Bytes victimName, Team killerTeam, Team victimTeam)
    {
        if (_killFeedPrefab == null || _killFeedContainer == null) return;
        KillFeedEntry entry = Instantiate(_killFeedPrefab, _killFeedContainer);

        Color kColor = GetTeamColor(killerTeam);
        Color vColor = GetTeamColor(victimTeam);

        entry.Setup(killerName.ToString(), victimName.ToString(), kColor, vColor);
    }

    private Color GetTeamColor(Team team)
    {
        return team switch
        {
            Team.Red => _redTeamColor,
            Team.Blue => _blueTeamColor,
            _ => _neutralColor
        };
    }
}