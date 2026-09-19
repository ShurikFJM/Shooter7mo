using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoleDatabase", menuName = "Rushpoint/Role Database")]
public class RoleDatabaseSO : ScriptableObject
{
    [SerializeField] private List<RoleDataSO> roles = new List<RoleDataSO>();

    public RoleDataSO GetRole(PlayerRoleType roleType)
    {
        for (int i = 0; i < roles.Count; i++)
        {
            if (roles[i] != null && roles[i].roleType == roleType)
                return roles[i];
        }
        return null;
    }
}