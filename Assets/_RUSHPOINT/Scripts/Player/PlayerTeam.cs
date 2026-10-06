using Unity.Netcode;
using UnityEngine;

public class PlayerTeam : NetworkBehaviour
{
    public NetworkVariable<Team> CurrentTeam = new NetworkVariable<Team>(
        Team.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void AssignTeam(Team team)
    {
        if (!IsServer) return;
        CurrentTeam.Value = team;
    }

    [Rpc(SendTo.Server)]
    public void DebugSetTeamServerRpc(Team team)
    {
        CurrentTeam.Value = team;
    }
}