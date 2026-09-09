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
    [Header("Configuración")]
    public HitboxType type = HitboxType.Chest;
    public NetworkHealth targetHealth;

    public float GetMultiplier()
    {
        switch (type)
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
        if (targetHealth != null)
        {
            float finalDamage = baseDamage * GetMultiplier();
            targetHealth.TakeDamage(finalDamage, type, attackerId);
        }
    }
}