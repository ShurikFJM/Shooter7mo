using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Rushpoint/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon")]
    public string weaponName = "Tactical Pistol";
    public bool isAutomatic = false;

    [Header("Combat")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.15f;

    [Header("Ammo")]
    public int maxAmmo = 12;
    public int maxReserveMagazines = 3;
    public float reloadTime = 1.5f;

    [Header("UI")]
    public Texture ammoIcon;

    [Header("Recoil")]
    public float recoilResetTime = 0.3f;

    public Vector2[] recoilPattern = new Vector2[]
    {
        new Vector2(0f, 0f),
        new Vector2(0f, 0.25f),
        new Vector2(0.05f, 0.5f),
        new Vector2(-0.1f, 0.8f)
    };

    [Header("Spread")]
    public float baseSpread = 0.0f;
    public float spreadPerShot = 0.08f;
    public float movementSpreadMultiplier = 3.5f;
    public float airSpreadMultiplier = 12.0f;

    [Header("Audio")]
    public AudioClip[] shootSounds;
    public AudioClip reloadSound;

    [Header("Impact")]
    public GameObject[] impactPrefabs;
}