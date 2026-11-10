using UnityEngine;

public enum PlayerRoleType
{
    Tank,
    Assault,
    Medic,
    Mobility,
    Sniper,
    Support
}

[CreateAssetMenu(fileName = "NewRoleData", menuName = "Rushpoint/Role Data")]
public class RoleDataSO : ScriptableObject
{
    public PlayerRoleType roleType;
    public string roleName;

    [TextArea(3, 6)]
    public string roleDescription;

    public float maxHealth = 100f;
    public float maxArmor = 50f;
    public float walkSpeed = 5.4f;
    public float jumpForce = 1.6f;

    public GameObject primaryWeaponPrefab;
    public GameObject secondaryWeaponPrefab;
    public GameObject tpPrimaryWeaponPrefab;
    public GameObject tpSecondaryWeaponPrefab;

    public GameObject thirdPersonModelPrefab;

    public Vector3 primaryPositionOffset = Vector3.zero;
    public Vector3 primaryRotationOffset = new Vector3(0f, 90f, 0f); 

    public Vector3 secondaryPositionOffset = Vector3.zero;
    public Vector3 secondaryRotationOffset = new Vector3(0f, 90f, 0f);

    public string primaryWeaponName = "Fusil de Asalto";
    [TextArea(2, 4)] public string primaryWeaponDescription = "Fusil automático de media y larga distancia.";
    public Sprite primaryWeaponIcon;

    public string secondaryWeaponName = "Pistola Táctica";
    [TextArea(2, 4)] public string secondaryWeaponDescription = "Arma secundaria para remates rápidos.";
    public Sprite secondaryWeaponIcon;

    public string utilityName = "Granada Táctica";
    [TextArea(2, 4)] public string utilityDescription = "Utilidad arrojadiza para control de zona.";
    public Sprite utilityIcon;

    [Header("Lobby Animation States")]
    public string primaryAnimState = "infantry_combat_idle";
    public string secondaryAnimState = "handgun_combat_idle";
}