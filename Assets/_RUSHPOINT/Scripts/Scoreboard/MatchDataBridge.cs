using System.Collections.Generic;

public static class MatchDataBridge
{
    public struct PlayerLobbyData
    {
        public string Name;
        public Team SelectedTeam;
    }

    public static Dictionary<ulong, PlayerLobbyData> PlayersCache = new Dictionary<ulong, PlayerLobbyData>();
}