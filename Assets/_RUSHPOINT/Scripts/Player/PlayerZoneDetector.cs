using Unity.Netcode;
using UnityEngine;

public class PlayerZoneDetector : NetworkBehaviour
{
    private const int _MAX_COLLIDER_BUFFER = 8;

    [SerializeField] private LayerMask _zoneLayer;
    [SerializeField] private float _detectRadius = 0.2f;

    private readonly Collider[] _colliderBuffer = new Collider[_MAX_COLLIDER_BUFFER];
    private DominationZone _currentZone;

    public DominationZone CurrentZone => _currentZone;

    private void Update()
    {
        if (!IsOwner) return;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _detectRadius, _colliderBuffer, _zoneLayer);
        _currentZone = null;

        for (int i = 0; i < hitCount; i++)
        {
            DominationZone zone = _colliderBuffer[i].GetComponent<DominationZone>();
            if (zone != null)
            {
                _currentZone = zone;
                break;
            }
        }
    }
}