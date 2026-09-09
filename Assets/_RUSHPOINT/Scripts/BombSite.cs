using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Delimita una zona donde la bomba puede ser plantada (ej. Sitio A / Sitio B).
/// Requiere un Collider marcado como Trigger. Colocar en un GameObject vacío
/// y ajustar el tamaño del collider al área deseada.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BombSite : NetworkBehaviour
{
    [Header("Identificación")]
    [Tooltip("Nombre corto del sitio, ej. 'A' o 'B'")]
    [SerializeField] private string siteId = "A";

    [Header("Punto de plantado")]
    [Tooltip("Transform exacto donde se posiciona la bomba al plantarla. Si se deja vacío, se usa este mismo objeto.")]
    [SerializeField] private Transform plantPoint;

    private Collider zoneCollider;
    private readonly HashSet<Collider> playersInZone = new HashSet<Collider>();

    public string SiteId => siteId;
    public Transform PlantPoint => plantPoint != null ? plantPoint : transform;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playersInZone.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playersInZone.Remove(other);
    }

    /// <summary>
    /// Usado por Bomb.cs (en el servidor) para validar que el jugador que
    /// intenta plantar realmente está dentro de los límites del sitio.
    /// </summary>
    public bool IsPositionInside(Vector3 worldPosition)
    {
        return zoneCollider.bounds.Contains(worldPosition);
    }

    /// <summary>
    /// Útil más adelante para lógica de defusa (requiere estar parado en el sitio).
    /// </summary>
    public bool HasAnyPlayerInside()
    {
        playersInZone.RemoveWhere(c => c == null);
        return playersInZone.Count > 0;
    }

    [ClientRpc]
    public void NotifyBombPlantedClientRpc()
    {
        Debug.Log($"[BombSite {siteId}] Bomba plantada en este sitio.");
        // Aquí se pueden disparar VFX/SFX locales, actualizar el HUD táctico, etc.
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}
