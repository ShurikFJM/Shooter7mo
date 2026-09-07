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
    public float recoilResetTime = 0.3f;
    public Vector2[] recoilPattern = new Vector2[]
    {
        new Vector2(0f, 0f),
        new Vector2(0f, 0.25f),
        new Vector2(0.05f, 0.5f),
        new Vector2(-0.1f, 0.8f)
    };

    [Header("Dispersión Táctica (Spread)")]
    public float baseSpread = 0.0f;
    public float spreadPerShot = 0.08f;
    public float movementSpreadMultiplier = 3.5f;
    public float airSpreadMultiplier = 12.0f;

    [Header("Audio (Variaciones Aleatorias)")]
    public AudioClip[] shootSounds; 
    public AudioClip reloadSound;    

    [Header("Efectos Visuales (Variaciones Aleatorias)")]
    public GameObject[] impactPrefabs; 
}