using Unity.Netcode;
using UnityEngine;

public class RoleLobbyManager : NetworkBehaviour
{
    public static RoleLobbyManager Instance { get; private set; }

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform[] _terroristSpawnPoints;
    [SerializeField] private Transform[] _counterTerroristSpawnPoints;

    private int _terroristSpawnIndex;
    private int _counterTerroristSpawnIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [Rpc(SendTo.Server)]
    public void LockInRoleAndTeamServerRpc(PlayerRoleType role, Team team, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out NetworkClient client))
        {
            return;
        }

        Transform targetSpawn = GetNextSpawnPoint(team);
        Vector3 spawnPosition = targetSpawn != null ? targetSpawn.position : Vector3.zero;
        Quaternion spawnRotation = targetSpawn != null ? targetSpawn.rotation : Quaternion.identity;

        NetworkObject playerNetworkObject = client.PlayerObject;

        if (playerNetworkObject == null && _playerPrefab != null)
        {
            GameObject spawnedPlayer = Instantiate(_playerPrefab, spawnPosition, spawnRotation);
            playerNetworkObject = spawnedPlayer.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnAsPlayerObject(senderId, true);
        }

        if (playerNetworkObject != null)
        {
            CharacterController characterController = playerNetworkObject.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;

            playerNetworkObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            if (characterController != null) characterController.enabled = true;

            NetworkPlayerController controller = playerNetworkObject.GetComponent<NetworkPlayerController>();
            if (controller != null)
            {
                controller.SetInitialRole(role);
            }

            PlayerTeam playerTeam = playerNetworkObject.GetComponent<PlayerTeam>();
            if (playerTeam != null)
            {
                playerTeam.CurrentTeam.Value = team;
            }
        }

        CloseRoleScreenForClientRpc(senderId);

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.NotifyPlayerLockedInServerRpc(senderId);
        }
    }

    private Transform GetNextSpawnPoint(Team team)
    {
        Transform[] spawns = (team == Team.Red) ? _terroristSpawnPoints : _counterTerroristSpawnPoints;
        if (spawns == null || spawns.Length == 0) return null;

        int index = (team == Team.Red) ? _terroristSpawnIndex++ : _counterTerroristSpawnIndex++;
        return spawns[index % spawns.Length];
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CloseRoleScreenForClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        RoleSelectScreenUI roleUI = FindAnyObjectByType<RoleSelectScreenUI>();
        if (roleUI != null)
        {
            roleUI.CloseRoleScreen();
        }

        GameObject lobbyCamera = GameObject.FindWithTag("LobbyCamera");
        if (lobbyCamera != null)
        {
            lobbyCamera.SetActive(false);
        }
    }
}