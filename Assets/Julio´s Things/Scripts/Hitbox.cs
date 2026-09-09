using UnityEngine;

public enum HitboxType
{
    Head,   // Multiplicador x4 (CS2 Headshot)
    Chest,  // Multiplicador x1
    Arms,   // Multiplicador x1
    Legs    // Multiplicador x0.75
}

public class Hitbox : MonoBehaviour
{
    [Header("Configuración de Hitbox")]
    public HitboxType type = HitboxType.Chest;
    public EnemyHealth enemyHealth; // Arrastrar el script principal de salud del enemigo

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

    public void ReceiveHit(float baseDamage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (enemyHealth != null)
        {
            float finalDamage = baseDamage * GetMultiplier();
            enemyHealth.TakeDamage(finalDamage, type, hitPoint, hitNormal);
        }
    }
}