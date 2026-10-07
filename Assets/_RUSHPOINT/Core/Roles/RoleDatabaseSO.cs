using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "RoleDatabase", menuName = "Rushpoint/Role Database")]
public class RoleDatabaseSO : ScriptableObject
{
    [FormerlySerializedAs("roles")]
    [SerializeField] private List<RoleDataSO> _roles = new List<RoleDataSO>();

    public RoleDataSO GetRole(PlayerRoleType roleType)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i] != null && _roles[i].roleType == roleType)
            {
                return _roles[i];
            }
        }
        return null;
    }
}