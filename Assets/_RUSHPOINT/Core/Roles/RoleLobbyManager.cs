using Unity.Netcode;
using UnityEngine;

public class RoleLobbyManager : NetworkBehaviour
{
    public static RoleLobbyManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void LockInRoleServerRpc(PlayerRoleType selectedRole, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Evitar que spawnee dos veces si hace spam de clics
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                Debug.LogWarning($"[Lobby] El cliente {clientId} ya spawneó en la partida.");
                return;
            }
        }

        // Determinar punto de aparición
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = (int)(clientId % (ulong)spawnPoints.Length);
            spawnPos = spawnPoints[index].position;
            spawnRot = spawnPoints[index].rotation;
        }

        // Instanciar el prefab del jugador
        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

        // Asignar el rol antes de spawnearlo en red
        NetworkPlayerController controller = playerInstance.GetComponent<NetworkPlayerController>();
        if (controller != null)
        {
            controller.SetInitialRole(selectedRole);
        }

        // Spawnear como el objeto oficial del jugador en red
        netObj.SpawnAsPlayerObject(clientId, true);
    }
}