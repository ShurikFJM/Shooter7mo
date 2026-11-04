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
            string playerName;

            if (MatchDataBridge.PlayersCache.TryGetValue(OwnerClientId, out var playerData))
            {
                CurrentTeam.Value = playerData.SelectedTeam;
                playerName = playerData.Name;
            }
            else
            {
                if (CurrentTeam.Value == Team.Neutral)
                {
                    CurrentTeam.Value = Team.Red;
                }
                playerName = $"Player_{OwnerClientId}";
            }
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.RegisterPlayerServer(OwnerClientId, playerName, CurrentTeam.Value);
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

        if (IsServer && ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.UpdatePlayerTeamServer(OwnerClientId, newTeam);
        }
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
}