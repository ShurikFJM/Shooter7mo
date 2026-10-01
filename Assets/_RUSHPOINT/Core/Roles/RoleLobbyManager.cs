using Unity.Netcode;
using UnityEngine;

public class RoleLobbyManager : NetworkBehaviour
{
    public static RoleLobbyManager Instance { get; private set; }

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;

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
    public void LockInRoleServerRpc(PlayerRoleType selectedRole, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient connectedClient))
        {
            if (connectedClient.PlayerObject != null)
            {
                return;
            }
        }

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int spawnIndex = (int)(clientId % (ulong)_spawnPoints.Length);
            spawnPosition = _spawnPoints[spawnIndex].position;
            spawnRotation = _spawnPoints[spawnIndex].rotation;
        }

        GameObject playerInstance = Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        NetworkPlayerController playerController = playerInstance.GetComponent<NetworkPlayerController>();
        if (playerController != null)
        {
            playerController.SetInitialRole(selectedRole);
        }

        networkObject.SpawnAsPlayerObject(clientId, true);
    }
}