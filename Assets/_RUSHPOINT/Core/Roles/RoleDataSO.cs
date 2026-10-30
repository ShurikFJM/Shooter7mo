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
    [TextArea] public string roleDescription;

    public float maxHealth = 100f;
    public float maxArmor = 50f;
    public float walkSpeed = 5.4f;
    public float jumpForce = 1.6f;

    public GameObject primaryWeaponPrefab;
    public GameObject secondaryWeaponPrefab;
    public GameObject tpPrimaryWeaponPrefab;
    public GameObject tpSecondaryWeaponPrefab;
}