using Unity.Netcode;
using UnityEngine;

public enum HitboxType
{
    Head,
    Chest,
    Legs
}

public class Hitbox : MonoBehaviour
{
    [SerializeField] private NetworkHealth _targetHealth;
    [SerializeField] private HitboxType _hitboxType = HitboxType.Chest;

    public NetworkHealth TargetHealth => _targetHealth;
    public HitboxType Type => _hitboxType;

    private void Awake()
    {
        if (_targetHealth == null)
        {
            _targetHealth = GetComponentInParent<NetworkHealth>();
            if (_targetHealth == null)
            {
                _targetHealth = transform.root.GetComponent<NetworkHealth>();
            }
        }
    }
}