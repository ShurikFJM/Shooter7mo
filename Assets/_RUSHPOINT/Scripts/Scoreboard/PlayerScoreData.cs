using System;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public struct PlayerScoreData : INetworkSerializable, IEquatable<PlayerScoreData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public Team PlayerTeam;
    public int Kills;
    public int Assists;
    public int Deaths;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref PlayerTeam);
        serializer.SerializeValue(ref Kills);
        serializer.SerializeValue(ref Assists);
        serializer.SerializeValue(ref Deaths);
    }

    public bool Equals(PlayerScoreData other) => ClientId == other.ClientId;
}