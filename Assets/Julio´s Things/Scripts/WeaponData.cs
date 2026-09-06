using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Rushpoint/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Información General")]
    public string weaponName = "Pistola Táctica";
    public bool isAutomatic = false;

    [Header("Estadísticas de Disparo")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.15f;

    [Header("Munición y Recarga")]
    public int maxAmmo = 12;
    public float reloadTime = 1.5f;

    [Header("Retroceso (Patrón CS2)")]
    public float recoilResetTime = 0.3f; // Tiempo para reiniciar el patrón al dejar de disparar
    public Vector2[] recoilPattern = new Vector2[]
    {
        new Vector2(0f, 0f),       // Bala 1: Centro exacto (0,0)
        new Vector2(0f, 0.25f),    // Bala 2: Sube levemente
        new Vector2(0.05f, 0.5f),  // Bala 3
        new Vector2(-0.1f, 0.8f),  // Bala 4
        new Vector2(-0.25f, 1.1f), // Bala 5
        new Vector2(0.2f, 1.2f)    // Bala 6
    };

    [Header("Dispersión Táctica (Spread)")]
    [Tooltip("Dispersión estando perfectamente quieto (0 = Precisión quirúrgica)")]
    public float baseSpread = 0.0f;

    [Tooltip("Aumento de dispersión por cada disparo consecutivo en ráfaga")]
    public float spreadPerShot = 0.005f;

    [Tooltip("Penalización de dispersión al caminar")]
    public float movementSpreadMultiplier = 0.04f;

    [Tooltip("Penalización masiva al estar en el aire")]
    public float airSpreadMultiplier = 0.12f;

    [Header("Efectos Visuales")]
    public GameObject impactPrefab;
}