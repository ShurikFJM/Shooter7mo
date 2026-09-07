using Unity.Netcode;
using UnityEngine;

public class NetworkBootstrapUI : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 220, 150));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Iniciar Host (Servidor + Jugador)", GUILayout.Height(35)))
                NetworkManager.Singleton.StartHost();

            if (GUILayout.Button("Iniciar Cliente", GUILayout.Height(35)))
                NetworkManager.Singleton.StartClient();

            if (GUILayout.Button("Iniciar Servidor Dedicado", GUILayout.Height(35)))
                NetworkManager.Singleton.StartServer();
        }
        else
        {
            GUILayout.Label($"Estado: {(NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Servidor" : "Cliente")}");
        }

        GUILayout.EndArea();
    }
}