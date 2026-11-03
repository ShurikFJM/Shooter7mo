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

    private void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            _playersInZone.Add(otherCollider);
        }
    }

    private void OnTriggerExit(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            _playersInZone.Remove(otherCollider);
        }
    }

    public bool IsPositionInside(Vector3 worldPosition)
    {
        return _zoneCollider.bounds.Contains(worldPosition);
    }

    public bool HasAnyPlayerInside()
    {
        _playersInZone.RemoveWhere(colliderElement => colliderElement == null);
        return _playersInZone.Count > 0;
    }

    [ClientRpc]
    public void NotifyBombPlantedClientRpc()
    {
    }

    private void OnDrawGizmos()
    {
        Collider siteCollider = GetComponent<Collider>();
        if (siteCollider == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (siteCollider is BoxCollider boxCollider)
        {
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else if (siteCollider is SphereCollider sphereCollider)
        {
            Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
        }
    }
}