using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Agregar junto a NetworkPlayerController / BombInteractor. Detecta en qué
/// DominationZone está parado el jugador local (mismo patrón que
/// BombInteractor.DetectCurrentSite). Solo corre para el owner, es puramente
/// para feedback visual — la lógica de captura real vive en DominationZone,
/// en el servidor.
/// </summary>
public class PlayerZoneDetector : NetworkBehaviour
{
    [SerializeField] private LayerMask zoneLayer;
    [SerializeField] private float detectRadius = 0.2f;

    private DominationZone currentZone;
    public DominationZone CurrentZone => currentZone;

    private void Update()
    {
        if (!IsOwner) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, zoneLayer);
        currentZone = null;

        foreach (Collider hit in hits)
        {
            DominationZone zone = hit.GetComponent<DominationZone>();
            if (zone != null)
            {
                currentZone = zone;
                break;
            }
        }
    }
}
