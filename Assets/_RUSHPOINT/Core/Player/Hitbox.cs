using Unity.Netcode;
using UnityEngine;

public enum HitboxType
{
    Head,
    Chest,
    Arms,
    Legs
}

public class Hitbox : MonoBehaviour
{
    [SerializeField] private HitboxType _type = HitboxType.Chest;
    [SerializeField] private NetworkHealth _targetHealth;

    public HitboxType Type => _type;
    public NetworkHealth TargetHealth => _targetHealth;

    public void ReceiveHit(float baseDamage, ulong attackerId)
    {
        if (_targetHealth == null) return;

        NetworkObject networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject != null && networkObject.OwnerClientId == attackerId)
        {
            return;
        }

        _targetHealth.TakeDamage(baseDamage, _type, attackerId);
    }
}