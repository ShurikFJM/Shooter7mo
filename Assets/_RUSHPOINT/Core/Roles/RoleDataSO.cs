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
    [Header("Role Identity")]
    public PlayerRoleType roleType;
    public string roleName;
    [TextArea] public string roleDescription;

    [Header("Health & Armor")]
    public float maxHealth = 100f;
    public float maxArmor = 50f;

    [Header("Movement Attributes")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 7.5f;
    public float jumpForce = 5f;
}