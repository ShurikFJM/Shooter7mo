using Unity.Netcode;
using UnityEngine;

public enum HitboxType
{
    Head,
    Chest,
    Legs
}

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] private NetworkHealth _targetHealth;
    [SerializeField] private HitboxType _hitboxType = HitboxType.Chest;

    public NetworkHealth TargetHealth => _targetHealth;
    public HitboxType Type => _hitboxType;

    private void Awake()
    {
        ResolveTargetHealth();
    }

    private void OnValidate()
    {
        if (_targetHealth == null)
        {
            ResolveTargetHealth();
        }
    }

    private void ResolveTargetHealth()
    {
        if (_targetHealth != null) return;

        _targetHealth = GetComponentInParent<NetworkHealth>();
        if (_targetHealth == null && transform.root != null)
        {
            _targetHealth = transform.root.GetComponentInChildren<NetworkHealth>(true);
        }
    }
}