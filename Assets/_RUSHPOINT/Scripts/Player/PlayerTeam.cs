using Unity.Netcode;
using UnityEngine;

public class PlayerTeam : NetworkBehaviour
{
    public NetworkVariable<Team> CurrentTeam = new NetworkVariable<Team>(
        Team.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (CustomLobbyManager.Instance != null)
            {
                Team lobbyAssignedTeam = CustomLobbyManager.Instance.GetPlayerAssignedTeam(OwnerClientId);
                if (lobbyAssignedTeam != Team.Neutral)
                {
                    CurrentTeam.Value = lobbyAssignedTeam;
                }
            }

            if (CurrentTeam.Value == Team.Neutral)
            {
                CurrentTeam.Value = Team.Red;
            }
        }

        CurrentTeam.OnValueChanged += HandleTeamChanged;
        CheckIfSpectator(CurrentTeam.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentTeam.OnValueChanged -= HandleTeamChanged;
        base.OnNetworkDespawn();
    }

    private void HandleTeamChanged(Team previousTeam, Team newTeam)
    {
        CheckIfSpectator(newTeam);
    }

    private void CheckIfSpectator(Team team)
    {
        if (team == Team.Neutral && IsOwner)
        {
            NetworkPlayerController controller = GetComponent<NetworkPlayerController>();
            CharacterController cc = GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false;

            if (controller != null && controller.PlayerCamera != null && SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.StartSpectating(
                    Team.Red,
                    controller.PlayerCamera,
                    transform,
                    1.6f
                );
            }
        }
    }

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