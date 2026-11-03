using System;
using Unity.Collections;
using Unity.Netcode;

public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public Team AssignedTeam;
    public bool IsReady;

    public LobbyPlayerData(ulong clientId, FixedString32Bytes playerName, Team team, bool isReady)
    {
        ClientId = clientId;
        PlayerName = playerName;
        AssignedTeam = team;
        IsReady = isReady;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref AssignedTeam);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(LobbyPlayerData other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               AssignedTeam == other.AssignedTeam &&
               IsReady == other.IsReady;
    }
}