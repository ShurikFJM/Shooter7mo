using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoleSelectScreenUI : MonoBehaviour
{
    [Header("Base de Datos")]
    [SerializeField] private RoleDatabaseSO roleDatabase;

    [Header("UI Info (Estilo Valorant)")]
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private TMP_Text roleNameText;
    [SerializeField] private TMP_Text roleDescText;
    [SerializeField] private TMP_Text healthStatsText;
    [SerializeField] private TMP_Text speedStatsText;
    [SerializeField] private Button lockInButton;

    private PlayerRoleType currentSelected = PlayerRoleType.Assault;
    private bool isLocked = false;

    private void Start()
    {
        ShowRoleDetails(PlayerRoleType.Assault);
        if (lockInButton != null)
        {
            lockInButton.onClick.AddListener(OnLockInClicked);
        }
    }

    public void OnRoleButtonClicked(int roleIndex)
    {
        if (isLocked) return;
        currentSelected = (PlayerRoleType)roleIndex;
        ShowRoleDetails(currentSelected);
    }

    private void ShowRoleDetails(PlayerRoleType roleType)
    {
        if (roleDatabase == null) return;

        RoleDataSO data = roleDatabase.GetRole(roleType);
        if (data == null) return;

        if (roleNameText != null) roleNameText.text = data.roleName.ToUpper();
        if (roleDescText != null) roleDescText.text = data.roleDescription;
        if (healthStatsText != null) healthStatsText.text = $"VIDA: {data.maxHealth} | ARMADURA: {data.maxArmor}";
        if (speedStatsText != null) speedStatsText.text = $"VELOCIDAD: {data.walkSpeed} m/s (Sprint: {data.sprintSpeed} m/s)";
    }

    private void OnLockInClicked()
    {
        if (isLocked) return;
        isLocked = true;

        if (RoleLobbyManager.Instance != null)
        {
            RoleLobbyManager.Instance.LockInRoleServerRpc(currentSelected);
        }

        // Apagar la pantalla de selección y bloquear cursor
        if (screenRoot != null) screenRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}