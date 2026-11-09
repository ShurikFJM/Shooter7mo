using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

[System.Serializable]
public class RoleButtonUI
{
    public PlayerRoleType RoleType;
    public Button RoleButton;
    public RectTransform RectTransform;
    public Image IconImage;
}

[System.Serializable]
public class RolePreviewModel
{
    public PlayerRoleType RoleType;
    public GameObject ModelRoot;
}

public class RoleSelectScreenUI : MonoBehaviour
{
    private const string HEALTH_PREFIX = "Vida: ";
    private const string ARMOR_PREFIX = "Armadura: ";
    private const string SPEED_PREFIX = "Velocidad: ";
    private const string ROOM_CODE_PREFIX = "ROOM CODE: ";

    public static RoleSelectScreenUI instance { get; private set; }

    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private RoleDatabaseSO _roleDatabase;
    [SerializeField] private GameObject _tacticalHudRoot;
    [SerializeField] private GameObject _minimapRoot;
    [SerializeField] private TMP_Text _roleNameText;
    [SerializeField] private TMP_Text _roleDescriptionText;
    [SerializeField] private TMP_Text _roleHealthText;
    [SerializeField] private TMP_Text _roleArmorText;
    [SerializeField] private TMP_Text _roleSpeedText;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _armorSlider;
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private float _sliderAnimationSpeed = 8f;
    [SerializeField] private TMP_Text _roomCodeText;
    [SerializeField] private Button _copyCodeButton;
    [SerializeField] private Button _lockInButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _selectTerroristButton;
    [SerializeField] private Button _selectCounterTerroristButton;
    [SerializeField] private Image _terroristButtonBackground;
    [SerializeField] private Image _counterTerroristButtonBackground;
    [SerializeField] private Color _teamSelectedColor = Color.white;
    [SerializeField] private Color _teamUnselectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private RoleButtonUI[] _roleButtons;
    [SerializeField] private float _radialRadius = 300f;
    [SerializeField] private float _angleSpacing = 25f;
    [SerializeField] private float _scaleFalloff = 0.2f;
    [SerializeField] private float _animationSpeed = 10f;
    [SerializeField] private Color _selectedRoleColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color _unselectedRoleColor = Color.white;
    [SerializeField] private RolePreviewModel[] _roleModels;

    private Team _selectedTeam = Team.Blue;
    private PlayerRoleType _selectedRole = PlayerRoleType.Assault;
    private int _selectedRoleIndex = 0;
    private bool _hasLockedIn = false;
    private string _currentJoinCode = string.Empty;
    private float _targetHealth;
    private float _targetArmor;
    private float _targetSpeed;

    public bool hasLockedIn => _hasLockedIn;
    public bool isRoleSelectionActive => !_hasLockedIn && _screenRoot != null && _screenRoot.activeSelf;
    public PlayerRoleType SelectedRole => _selectedRole;
    public Team SelectedTeam => _selectedTeam;
    public RoleDatabaseSO RoleDatabase => _roleDatabase;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        BindButtonCallbacks();

        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(false);
        if (_minimapRoot != null) _minimapRoot.SetActive(false);
    }

    private void Start()
    {
        if (!_hasLockedIn) OpenRoleScreen();

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
        if (!_hasLockedIn) OpenRoleScreen();
    }

    private void Update()
    {
        HandleNetworkButtons();
        AnimateRadialMenu();
        AnimateSliders();
    }

    private void HandleNetworkButtons()
    {
        bool canLockIn = !_hasLockedIn && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
        if (_confirmButton != null) _confirmButton.interactable = canLockIn;
        if (_lockInButton != null) _lockInButton.interactable = canLockIn;
    }

    private void AnimateRadialMenu()
    {
        if (_roleButtons == null || _roleButtons.Length == 0) return;

        for (int i = 0; i < _roleButtons.Length; i++)
        {
            RoleButtonUI btn = _roleButtons[i];
            if (btn.RectTransform == null) continue;

            float diff = i - _selectedRoleIndex;
            float angleDeg = diff * _angleSpacing;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float targetY = -Mathf.Sin(angleRad) * _radialRadius;
            float targetX = Mathf.Cos(angleRad) * _radialRadius - _radialRadius;
            Vector2 targetPos = new Vector2(targetX, targetY);

            float targetScaleVal = 1f - (Mathf.Abs(diff) * _scaleFalloff);
            targetScaleVal = Mathf.Clamp(targetScaleVal, 0.5f, 1.2f);
            Vector3 targetScale = new Vector3(targetScaleVal, targetScaleVal, 1f);

            Color targetColor = (i == _selectedRoleIndex) ? _selectedRoleColor : _unselectedRoleColor;
            float lerpT = Time.deltaTime * _animationSpeed;

            btn.RectTransform.anchoredPosition = Vector2.Lerp(btn.RectTransform.anchoredPosition, targetPos, lerpT);
            btn.RectTransform.localScale = Vector3.Lerp(btn.RectTransform.localScale, targetScale, lerpT);

            if (btn.IconImage != null)
            {
                btn.IconImage.color = Color.Lerp(btn.IconImage.color, targetColor, lerpT);
            }
        }
    }

    private void AnimateSliders()
    {
        if (_healthSlider != null)
            _healthSlider.value = Mathf.Lerp(_healthSlider.value, _targetHealth, Time.deltaTime * _sliderAnimationSpeed);

        if (_armorSlider != null)
            _armorSlider.value = Mathf.Lerp(_armorSlider.value, _targetArmor, Time.deltaTime * _sliderAnimationSpeed);

        if (_speedSlider != null)
            _speedSlider.value = Mathf.Lerp(_speedSlider.value, _targetSpeed, Time.deltaTime * _sliderAnimationSpeed);
    }

    private void SnapSliders()
    {
        if (_healthSlider != null) _healthSlider.value = _targetHealth;
        if (_armorSlider != null) _armorSlider.value = _targetArmor;
        if (_speedSlider != null) _speedSlider.value = _targetSpeed;
    }

    private void BindButtonCallbacks()
    {
        if (_selectTerroristButton != null) _selectTerroristButton.onClick.AddListener(() => SelectTeam(Team.Red));
        if (_selectCounterTerroristButton != null) _selectCounterTerroristButton.onClick.AddListener(() => SelectTeam(Team.Blue));
        if (_confirmButton != null) _confirmButton.onClick.AddListener(ConfirmSelection);
        if (_lockInButton != null) _lockInButton.onClick.AddListener(ConfirmSelection);
        if (_copyCodeButton != null) _copyCodeButton.onClick.AddListener(CopyRoomCodeToClipboard);

        for (int i = 0; i < _roleButtons.Length; i++)
        {
            int index = i;
            if (_roleButtons[i].RoleButton != null)
            {
                _roleButtons[i].RoleButton.onClick.AddListener(() =>
                {
                    _selectedRoleIndex = index;
                    SelectRolePreview(_roleButtons[index].RoleType);
                });
            }
        }
    }

    public void SelectRolePreview(PlayerRoleType role)
    {
        if (_hasLockedIn) return;
        _selectedRole = role;

        Update3DModel(role);

        if (_roleDatabase == null) return;
        RoleDataSO roleData = _roleDatabase.GetRole(role);
        if (roleData == null) return;

        if (_roleNameText != null) _roleNameText.text = roleData.roleName;
        if (_roleDescriptionText != null) _roleDescriptionText.text = roleData.roleDescription;
        if (_roleHealthText != null) _roleHealthText.text = string.Concat(HEALTH_PREFIX, roleData.maxHealth, "/100");
        if (_roleArmorText != null) _roleArmorText.text = string.Concat(ARMOR_PREFIX, roleData.maxArmor, "/100");
        if (_roleSpeedText != null) _roleSpeedText.text = string.Concat(SPEED_PREFIX, roleData.walkSpeed.ToString("F1"), "%");

        _targetHealth = roleData.maxHealth;
        _targetArmor = roleData.maxArmor;
        _targetSpeed = roleData.walkSpeed;
    }

    private void Update3DModel(PlayerRoleType role)
    {
        if (_roleModels == null) return;

        foreach (var modelData in _roleModels)
        {
            if (modelData.ModelRoot != null)
            {
                modelData.ModelRoot.SetActive(modelData.RoleType == role);
            }
        }
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
        if (_terroristButtonBackground != null) _terroristButtonBackground.color = isTerrorist ? _teamSelectedColor : _teamUnselectedColor;
        if (_counterTerroristButtonBackground != null) _counterTerroristButtonBackground.color = !isTerrorist ? _teamSelectedColor : _teamUnselectedColor;
    }

    public void ConfirmSelection()
    {
        if (_hasLockedIn) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        _hasLockedIn = true;
        SetLockInText(_confirmButton);
        SetLockInText(_lockInButton);

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        CustomLobbyManager.SavePlayerData(localClientId, _selectedTeam, _selectedRole);

        if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            var controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
            if (controller != null) controller.RequestSelectRoleServerRpc(_selectedRole);

            var playerTeam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerTeam>();
            if (playerTeam != null && NetworkManager.Singleton.IsServer)
            {
                playerTeam.CurrentTeam.Value = _selectedTeam;
            }

            var playerCanvas = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Canvas>(true);
            if (playerCanvas != null) playerCanvas.gameObject.SetActive(true);
        }

        if (RoleLobbyManager.Instance != null && RoleLobbyManager.Instance.IsSpawned)
        {
            RoleLobbyManager.Instance.ConfirmRoleSelection(_selectedRole, _selectedTeam);
        }

        CloseRoleScreen();
        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(true);
        if (_minimapRoot != null) _minimapRoot.SetActive(true);
    }

    private void SetLockInText(Button btn)
    {
        if (btn == null) return;
        btn.interactable = false;
        TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = "LOCKED IN...";
    }

    public void OpenRoleScreen()
    {
        if (_screenRoot != null) _screenRoot.SetActive(true);
        if (_tacticalHudRoot != null) _tacticalHudRoot.SetActive(false);
        if (_minimapRoot != null) _minimapRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CustomLobbyManager.Instance != null && NetworkManager.Singleton != null)
        {
            Team assignedTeam = CustomLobbyManager.Instance.GetPlayerAssignedTeam(NetworkManager.Singleton.LocalClientId);
            if (assignedTeam != Team.Neutral) _selectedTeam = assignedTeam;
        }

        UpdateTeamFeedbackUI();

        for (int i = 0; i < _roleButtons.Length; i++)
        {
            if (_roleButtons[i].RoleType == _selectedRole)
            {
                _selectedRoleIndex = i;
                break;
            }
        }

        SelectRolePreview(_selectedRole);
        SnapSliders(); 

        if (!string.IsNullOrEmpty(_currentJoinCode) && _roomCodeText != null)
        {
            _roomCodeText.text = string.Concat(ROOM_CODE_PREFIX, _currentJoinCode);
        }
    }

    public void CloseRoleScreen()
    {
        if (_screenRoot != null) _screenRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetRoomCode(string code)
    {
        _currentJoinCode = code;
        if (_roomCodeText != null) _roomCodeText.text = string.Concat(ROOM_CODE_PREFIX, code);
    }

    public void CopyRoomCodeToClipboard()
    {
        if (!string.IsNullOrEmpty(_currentJoinCode)) GUIUtility.systemCopyBuffer = _currentJoinCode;
    }

    private void HandlePhaseChanged(RoundPhase previousPhase, RoundPhase currentPhase)
    {
        if (_hasLockedIn && (currentPhase == RoundPhase.Warmup || currentPhase == RoundPhase.InProgress))
        {
            CloseRoleScreen();
        }
    }
}