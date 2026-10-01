using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoleSelectScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private RoleDatabaseSO _roleDatabase;

    [SerializeField] private TMP_Text _roleNameText;
    [SerializeField] private TMP_Text _roleDescriptionText;
    [SerializeField] private TMP_Text _roleHealthText;
    [SerializeField] private TMP_Text _roleArmorText;
    [SerializeField] private TMP_Text _roleSpeedText;

    [SerializeField] private Button _confirmButton;

    private int _selectedRoleIndex;

    private void Awake()
    {
        EnableSelectionInterface();
    }

    private void OnEnable()
    {
        EnableSelectionInterface();
    }

    public void EnableSelectionInterface()
    {
        if (_screenRoot != null)
        {
            _screenRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SelectRolePreview(0);
    }

    public void OnRoleButtonClicked(int roleIndex)
    {
        SelectRolePreview(roleIndex);
    }

    public void SelectRolePreview(int roleIndex)
    {
        _selectedRoleIndex = roleIndex;

        if (_roleDatabase == null) return;

        PlayerRoleType roleType = (PlayerRoleType)roleIndex;
        RoleDataSO roleData = _roleDatabase.GetRole(roleType);

        if (roleData == null) return;

        if (_roleNameText != null) _roleNameText.text = roleData.roleName;
        if (_roleDescriptionText != null) _roleDescriptionText.text = roleData.roleDescription;
        if (_roleHealthText != null) _roleHealthText.text = $"Health: {roleData.maxHealth}";
        if (_roleArmorText != null) _roleArmorText.text = $"Armor: {roleData.maxArmor}";
        if (_roleSpeedText != null) _roleSpeedText.text = $"Speed: {roleData.walkSpeed:F1}";

        if (_confirmButton != null)
        {
            _confirmButton.interactable = true;
        }
    }

    public void OnLockInButtonClicked()
    {
        ConfirmSelectedRole();
    }

    public void ConfirmSelectedRole()
    {
        PlayerRoleType selectedRoleType = (PlayerRoleType)_selectedRoleIndex;

        if (RoleLobbyManager.Instance != null)
        {
            RoleLobbyManager.Instance.LockInRoleServerRpc(selectedRoleType);
        }

        if (_screenRoot != null)
        {
            _screenRoot.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}