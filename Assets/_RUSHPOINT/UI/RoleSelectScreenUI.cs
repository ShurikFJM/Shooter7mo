using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class RoleSelectScreenUI : MonoBehaviour
{
    private const string _HEALTH_PREFIX = "Health: ";
    private const string _ARMOR_PREFIX = "Armor: ";
    private const string _SPEED_PREFIX = "Speed: ";

    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private RoleDatabaseSO _roleDatabase;

    [SerializeField] private TMP_Text _roleNameText;
    [SerializeField] private TMP_Text _roleDescriptionText;
    [SerializeField] private TMP_Text _roleHealthText;
    [SerializeField] private TMP_Text _roleArmorText;
    [SerializeField] private TMP_Text _roleSpeedText;
    [SerializeField] private Button _lockInButton;

    [SerializeField] private Button _selectTerroristButton;
    [SerializeField] private Button _selectCounterTerroristButton;

    [SerializeField] private Image _terroristButtonBackground;
    [SerializeField] private Image _counterTerroristButtonBackground;

    [SerializeField] private Color _teamSelectedColor = Color.white;
    [SerializeField] private Color _teamUnselectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [SerializeField] private Button _selectAssaultButton;
    [SerializeField] private Button _selectMedicButton;
    [SerializeField] private Button _selectMobilityButton;
    [SerializeField] private Button _selectSniperButton;
    [SerializeField] private Button _selectSupportButton;
    [SerializeField] private Button _selectTankButton;

    [SerializeField] private Button _confirmButton;

    private TeamSide _selectedTeam = TeamSide.CounterTerrorist;
    private PlayerRoleType _selectedRole = PlayerRoleType.Assault;

    private void Awake()
    {
        BindButtonCallbacks();
    }

    private void Start()
    {
        OpenRoleScreen();
    }

    private void OnEnable()
    {
        OpenRoleScreen();
    }

    private void BindButtonCallbacks()
    {
        if (_selectTerroristButton != null)
        {
            _selectTerroristButton.onClick.AddListener(() => SelectTeam(TeamSide.Terrorist));
        }

        if (_selectCounterTerroristButton != null)
        {
            _selectCounterTerroristButton.onClick.AddListener(() => SelectTeam(TeamSide.CounterTerrorist));
        }

        if (_selectAssaultButton != null)
        {
            _selectAssaultButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Assault));
        }

        if (_selectMedicButton != null)
        {
            _selectMedicButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Medic));
        }

        if (_selectMobilityButton != null)
        {
            _selectMobilityButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Mobility));
        }

        if (_selectSniperButton != null)
        {
            _selectSniperButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Sniper));
        }

        if (_selectSupportButton != null)
        {
            _selectSupportButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Support));
        }

        if (_selectTankButton != null)
        {
            _selectTankButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Tank));
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(ConfirmSelection);
        }
    }

    public void SelectTeam(TeamSide team)
    {
        _selectedTeam = team;
        UpdateTeamFeedbackUI();
    }

    private void UpdateTeamFeedbackUI()
    {
        bool isTerrorist = _selectedTeam == TeamSide.Terrorist;

        if (_terroristButtonBackground != null)
        {
            _terroristButtonBackground.color = isTerrorist ? _teamSelectedColor : _teamUnselectedColor;
        }

        if (_counterTerroristButtonBackground != null)
        {
            _counterTerroristButtonBackground.color = !isTerrorist ? _teamSelectedColor : _teamUnselectedColor;
        }
    }

    public void SelectRolePreview(PlayerRoleType role)
    {
        _selectedRole = role;

        if (_roleDatabase == null) return;

        RoleDataSO roleData = _roleDatabase.GetRole(role);
        if (roleData == null) return;

        if (_roleNameText != null) _roleNameText.text = roleData.roleName;
        if (_roleDescriptionText != null) _roleDescriptionText.text = roleData.roleDescription;
        if (_roleHealthText != null) _roleHealthText.text = string.Concat(_HEALTH_PREFIX, roleData.maxHealth);
        if (_roleArmorText != null) _roleArmorText.text = string.Concat(_ARMOR_PREFIX, roleData.maxArmor);
        if (_roleSpeedText != null) _roleSpeedText.text = string.Concat(_SPEED_PREFIX, roleData.walkSpeed.ToString("F1"));

        if (_confirmButton != null)
        {
            _confirmButton.interactable = true;
        }
    }

    public void OnClickLockInRole()
    {
        if (_lockInButton != null)
        {
            _lockInButton.interactable = false;
        }

        if (RoundManager.Instance != null && NetworkManager.Singleton != null)
        {
            RoundManager.Instance.NotifyPlayerLockedInServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void ConfirmSelection()
    {
        if (RoleLobbyManager.Instance != null)
        {
            RoleLobbyManager.Instance.LockInRoleAndTeamServerRpc(_selectedRole, _selectedTeam);
        }

        CloseRoleScreen();
    }

    public void OpenRoleScreen()
    {
        if (_screenRoot != null)
        {
            _screenRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateTeamFeedbackUI();
        SelectRolePreview(_selectedRole);
    }

    public void CloseRoleScreen()
    {
        if (_screenRoot != null)
        {
            _screenRoot.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}