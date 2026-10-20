using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Rushpoint/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Tactical Pistol";
    public bool isAutomatic = false;
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.15f;
    public int maxAmmo = 12;
    public int maxReserveMagazines = 3;
    public float reloadTime = 1.5f;
    public Texture ammoIcon;
    public float recoilResetTime = 0.3f;
    public Vector2[] recoilPattern = new Vector2[]
    {
        new Vector2(0f, 0f),
        new Vector2(0f, 0.25f),
        new Vector2(0.05f, 0.5f),
        new Vector2(-0.1f, 0.8f)
    };
    public float baseSpread = 0.0f;
    public float spreadPerShot = 0.004f;
    public float movementSpreadMultiplier = 1.8f;
    public float airSpreadMultiplier = 4.5f;
    public AudioClip[] shootSounds;
    public AudioClip reloadSound;
    public AudioClip[] wallImpactSounds;
    public AudioClip[] playerImpactSounds;
    public GameObject[] impactPrefabs;
    public GameObject muzzleFlashVfxPrefab;
    public GameObject wallImpactVfxPrefab;
    public GameObject playerImpactVfxPrefab;
    public string muzzleFlashAddressKey = "VFX_MuzzleFlash";
    public string wallImpactAddressKey = "VFX_Impact_Concrete";
    public string playerImpactAddressKey = "VFX_Impact_Blood";
}