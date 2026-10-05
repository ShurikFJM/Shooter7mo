using Unity.Netcode;
using UnityEngine;

public class RoleLobbyManager : NetworkBehaviour
{
    public static RoleLobbyManager Instance { get; private set; }

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform[] _terroristSpawnPoints;
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LockInRoleAndTeamServerRpc(PlayerRoleType selectedRole, TeamSide selectedTeam, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient connectedClient))
        {
            if (connectedClient.PlayerObject != null)
            {
                return;
            }
        }

        Transform[] targetSpawnList = selectedTeam == TeamSide.Terrorist
            ? _terroristSpawnPoints
            : _counterTerroristSpawnPoints;

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (targetSpawnList != null && targetSpawnList.Length > 0)
        {
            int spawnIndex = (int)(clientId % (ulong)targetSpawnList.Length);
            spawnPosition = targetSpawnList[spawnIndex].position;
            spawnRotation = targetSpawnList[spawnIndex].rotation;
        }

        GameObject playerInstance = Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        networkObject.SpawnAsPlayerObject(clientId, true);

        NetworkPlayerController playerController = playerInstance.GetComponent<NetworkPlayerController>();
        if (playerController != null)
        {
            playerController.SetInitialRole(selectedRole);
        }

        PlayerTeam playerTeam = playerInstance.GetComponent<PlayerTeam>();
        if (playerTeam != null)
        {
            playerTeam.CurrentTeam.Value = selectedTeam == TeamSide.Terrorist ? Team.Red : Team.Blue;
        }

        NotifyPlayerSpawnedClientRpc(clientId, selectedTeam);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyPlayerSpawnedClientRpc(ulong clientId, TeamSide team)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        if (TacticalChatManager.Instance != null)
        {
            TacticalChatManager.Instance.localTeam = team;
        }

        RoleSelectScreenUI roleUI = FindAnyObjectByType<RoleSelectScreenUI>();
        if (roleUI != null)
        {
            roleUI.CloseRoleScreen();
        }
    }
}