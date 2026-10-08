using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Pantalla de seleccion de rol/equipo. En modo tutorial no depende de la red
/// y solo avisa (evento RoleConfirmed) de lo que el jugador eligio.
/// </summary>
public class RoleSelectScreenUI : MonoBehaviour
{
    private const string _HEALTH_PREFIX = "Health: ";
    private const string _ARMOR_PREFIX = "Armor: ";
    private const string _SPEED_PREFIX = "Speed: ";

    [Header("Behaviour")]
    [Tooltip("Abre la pantalla sola en OnEnable/Start. El TutorialManager lo desactiva en modo tutorial.")]
    [SerializeField] private bool _autoOpenOnEnable = true;

    [Header("References")]
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
    private bool _buttonsBound = false;
    private bool _phaseSubscribed = false;
    private bool _tutorialMode = false;

    public bool HasLockedIn => _hasLockedIn;

    /// <summary>Se dispara al confirmar la seleccion (en tutorial y en partida).</summary>
    public event Action<PlayerRoleType, Team> RoleConfirmed;

    // ------------------------------------------------------------------ lifecycle

    private void Awake()
    {
        BindButtonCallbacks();

        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(false);
        if (_minimapRoot != null) _minimapRoot.SetActive(false);
    }

    private void Start()
    {
        if (_autoOpenOnEnable && !_hasLockedIn)
        {
            OpenRoleScreen();
        }

        SubscribeToPhase();
    }

    private void OnEnable()
    {
        if (_autoOpenOnEnable && !_hasLockedIn)
        {
            OpenRoleScreen();
        }

        SubscribeToPhase();
    }

    private void OnDisable()
    {
        UnsubscribeFromPhase();
    }

    private void OnDestroy()
    {
        UnsubscribeFromPhase();
    }

    private void Update()
    {
        bool connected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
        bool canInteract = !_hasLockedIn && (_tutorialMode || connected);

        if (_confirmButton != null) _confirmButton.interactable = canInteract;
        if (_lockInButton != null) _lockInButton.interactable = canInteract;
    }

    // ------------------------------------------------------------------ orders from outside

    /// <summary>
    /// En modo tutorial la pantalla no depende de la red ni se abre sola.
    /// </summary>
    public void SetTutorialMode(bool enabled)
    {
        _tutorialMode = enabled;

        if (enabled)
        {
            _autoOpenOnEnable = false;

            // En el tutorial el equipo lo controla el TutorialManager.
            if (_selectTerroristButton != null) _selectTerroristButton.gameObject.SetActive(false);
            if (_selectCounterTerroristButton != null) _selectCounterTerroristButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Reabre la pantalla aunque ya se hubiera hecho lock-in y aunque el
    /// Canvas estuviera desactivado.
    /// </summary>
    public void ForceOpen()
    {
        _hasLockedIn = false;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        BindButtonCallbacks();
        OpenRoleScreen();
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

    // ------------------------------------------------------------------ phase

    private void SubscribeToPhase()
    {
        if (_phaseSubscribed) return;
        if (RoundManager.Instance == null) return;

        RoundManager.Instance.CurrentPhase.OnValueChanged += HandlePhaseChanged;
        _phaseSubscribed = true;
    }

    private void UnsubscribeFromPhase()
    {
        if (!_phaseSubscribed) return;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.CurrentPhase.OnValueChanged -= HandlePhaseChanged;
        }

        _phaseSubscribed = false;
    }

    private void HandlePhaseChanged(RoundPhase previousPhase, RoundPhase currentPhase)
    {
        if (_hasLockedIn && (currentPhase == RoundPhase.Warmup || currentPhase == RoundPhase.InProgress))
        {
            CloseRoleScreen();
        }
    }

    // ------------------------------------------------------------------ buttons

    private void BindButtonCallbacks()
    {
        if (_buttonsBound) return;
        _buttonsBound = true;

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

        NetworkManager nm = NetworkManager.Singleton;
        bool networkReady = nm != null && nm.IsListening;

        // Fuera del tutorial seguimos exigiendo red.
        if (!_tutorialMode && !networkReady) return;

        _hasLockedIn = true;

        if (_confirmButton != null) _confirmButton.interactable = false;
        if (_lockInButton != null) _lockInButton.interactable = false;

        // En tutorial NO se usa RoleLobbyManager (teletransporta a spawns y llama a RoundManager).
        if (networkReady && !_tutorialMode && RoleLobbyManager.Instance != null)
        {
            RoleLobbyManager.Instance.LockInRoleAndTeamServerRpc(_selectedRole, _selectedTeam);
        }

        CloseRoleScreen();

        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(true);
        if (_minimapRoot != null) _minimapRoot.SetActive(true);

        if (networkReady && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
        {
            Canvas playerCanvas = nm.LocalClient.PlayerObject.GetComponentInChildren<Canvas>(true);
            if (playerCanvas != null)
            {
                playerCanvas.gameObject.SetActive(true);
            }
        }

        RoleConfirmed?.Invoke(_selectedRole, _selectedTeam);
    }
}   