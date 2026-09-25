using Unity.Netcode;
using UnityEngine;

public enum Team : byte
{
    Neutral,
    Red,
    Blue
}

/// <summary>
/// Identifica a qué equipo pertenece un jugador. Colocar en el mismo GameObject
/// que NetworkPlayerController. La asignación real de equipo la debe hacer tu
/// sistema de matchmaking/game manager llamando AssignTeam() desde el servidor
/// al iniciar la partida; se incluye un ServerRpc de debug para poder probar
/// en el editor mientras ese sistema no exista.
/// </summary>
public class PlayerTeam : NetworkBehaviour
{
    public NetworkVariable<Team> CurrentTeam = new NetworkVariable<Team>(
        Team.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>Llamar solo desde el servidor (ej. desde tu GameManager al iniciar la partida).</summary>
    public void AssignTeam(Team team)
    {
        if (!IsServer) return;
        CurrentTeam.Value = team;
    }

    // --- Solo para pruebas rápidas en el editor mientras no exista el sistema real de equipos ---
    [ServerRpc]
    public void DebugSetTeamServerRpc(Team team)
    {
        CurrentTeam.Value = team;
    }
}
