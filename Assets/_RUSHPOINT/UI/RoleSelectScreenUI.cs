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
    [SerializeField] private GameObject _tacticalHudRoot;
    [SerializeField] private GameObject _minimapRoot;

    private Team _selectedTeam = Team.Blue;
    private PlayerRoleType _selectedRole = PlayerRoleType.Assault;
    private bool _hasLockedIn = false;

    public bool HasLockedIn => _hasLockedIn;

    private void Awake()
    {
        BindButtonCallbacks();

        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(false);
        if (_minimapRoot != null) _minimapRoot.SetActive(false);
    }

    private void Start()
    {
        if (!_hasLockedIn)
        {
            OpenRoleScreen();
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.CurrentPhase.OnValueChanged += HandlePhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.CurrentPhase.OnValueChanged -= HandlePhaseChanged;
        }
    }

    private void OnEnable()
    {
        if (!_hasLockedIn)
        {
            OpenRoleScreen();
        }
    }

    private void Update()
    {
        if (_confirmButton != null && NetworkManager.Singleton != null)
        {
            _confirmButton.interactable = !_hasLockedIn && NetworkManager.Singleton.IsConnectedClient;
        }

        if (_lockInButton != null && NetworkManager.Singleton != null)
        {
            _lockInButton.interactable = !_hasLockedIn && NetworkManager.Singleton.IsConnectedClient;
        }
    }

    private void HandlePhaseChanged(RoundPhase previousPhase, RoundPhase currentPhase)
    {
        if (_hasLockedIn && (currentPhase == RoundPhase.Warmup || currentPhase == RoundPhase.InProgress))
        {
            CloseRoleScreen();
        }
    }

    private void BindButtonCallbacks()
    {
        if (_selectTerroristButton != null) _selectTerroristButton.onClick.AddListener(() => SelectTeam(Team.Red));
        if (_selectCounterTerroristButton != null) _selectCounterTerroristButton.onClick.AddListener(() => SelectTeam(Team.Blue));
        if (_selectAssaultButton != null) _selectAssaultButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Assault));
        if (_selectMedicButton != null) _selectMedicButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Medic));
        if (_selectMobilityButton != null) _selectMobilityButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Mobility));
        if (_selectSniperButton != null) _selectSniperButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Sniper));
        if (_selectSupportButton != null) _selectSupportButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Support));
        if (_selectTankButton != null) _selectTankButton.onClick.AddListener(() => SelectRolePreview(PlayerRoleType.Tank));
        if (_confirmButton != null) _confirmButton.onClick.AddListener(ConfirmSelection);
        if (_lockInButton != null) _lockInButton.onClick.AddListener(ConfirmSelection);
    }

    public void SelectTeam(Team team)
    {
        if (_hasLockedIn) return;
        _selectedTeam = team;
        UpdateTeamFeedbackUI();
    }

    private void UpdateTeamFeedbackUI()
    {
        bool isTerrorist = _selectedTeam == Team.Red;

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
        if (_hasLockedIn) return;
        _selectedRole = role;

        if (_roleDatabase == null) return;

        RoleDataSO roleData = _roleDatabase.GetRole(role);
        if (roleData == null) return;

        if (_roleNameText != null) _roleNameText.text = roleData.roleName;
        if (_roleDescriptionText != null) _roleDescriptionText.text = roleData.roleDescription;
        if (_roleHealthText != null) _roleHealthText.text = string.Concat(_HEALTH_PREFIX, roleData.maxHealth);
        if (_roleArmorText != null) _roleArmorText.text = string.Concat(_ARMOR_PREFIX, roleData.maxArmor);
        if (_roleSpeedText != null) _roleSpeedText.text = string.Concat(_SPEED_PREFIX, roleData.walkSpeed.ToString("F1"));
    }

    public void ConfirmSelection()
    {
        if (_hasLockedIn) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        _hasLockedIn = true;

        if (_confirmButton != null) _confirmButton.interactable = false;
        if (_lockInButton != null) _lockInButton.interactable = false;

        if (RoleLobbyManager.Instance != null)
        {
            RoleLobbyManager.Instance.LockInRoleAndTeamServerRpc(_selectedRole, _selectedTeam);
        }

        CloseRoleScreen();

        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(true);
        if (_minimapRoot != null) _minimapRoot.SetActive(true);

        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            Canvas playerCanvas = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Canvas>(true);
            if (playerCanvas != null)
            {
                playerCanvas.gameObject.SetActive(true);
            }
        }
    }

    public void OpenRoleScreen()
    {
        if (_screenRoot != null)
        {
            _screenRoot.SetActive(true);
        }

        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(false);
        if (_minimapRoot != null) _minimapRoot.SetActive(false);

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