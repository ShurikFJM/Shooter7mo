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

    public float GetMultiplier()
    {
        switch (_type)
        {
            case HitboxType.Head: return 4.0f;
            case HitboxType.Chest: return 1.0f;
            case HitboxType.Arms: return 1.0f;
            case HitboxType.Legs: return 0.75f;
            default: return 1.0f;
        }
    }

    public void ReceiveHit(float baseDamage, ulong attackerId)
    {
        if (_targetHealth == null) return;

        NetworkObject networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject != null && networkObject.OwnerClientId == attackerId)
        {
            return;
        }

        float finalDamage = baseDamage * GetMultiplier();
        _targetHealth.TakeDamage(finalDamage, _type, attackerId);
    }
}