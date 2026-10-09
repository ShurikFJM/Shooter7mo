using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BombSite : NetworkBehaviour
{
    [SerializeField] private string _siteId = "A";
    [SerializeField] private Transform _plantPoint;

    private Collider _zoneCollider;
    private readonly HashSet<Collider> _playersInZone = new HashSet<Collider>();

    public string SiteId => _siteId;
    public Transform PlantPoint => _plantPoint != null ? _plantPoint : transform;

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider>();
        _zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _playersInZone.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _playersInZone.Remove(other);
    }

    public bool IsPositionInside(Vector3 worldPosition)
    {
        return _zoneCollider.bounds.Contains(worldPosition);
    }

    public bool HasAnyPlayerInside()
    {
        _playersInZone.RemoveWhere(c => c == null);
        return _playersInZone.Count > 0;
    }

    [ClientRpc]
    public void NotifyBombPlantedClientRpc()
    {
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