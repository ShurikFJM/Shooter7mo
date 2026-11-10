using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    public Transform HandSocketOverride;
}

public class RoleSelectScreenUI : MonoBehaviour
{
    private const string HEALTH_PREFIX = "Vida: ";
    private const string ARMOR_PREFIX = "Armadura: ";
    private const string SPEED_PREFIX = "Velocidad: ";

    public static RoleSelectScreenUI instance { get; private set; }

    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private RoleDatabaseSO _roleDatabase;

    [SerializeField] private TMP_Text _roleNameText;
    [SerializeField] private TMP_Text _roleDescriptionText;
    [SerializeField] private TMP_Text _roleHealthText;
    [SerializeField] private TMP_Text _roleArmorText;
    [SerializeField] private TMP_Text _roleSpeedText;

    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _armorSlider;
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private float _sliderAnimationSpeed = 8f;

    [SerializeField] private Button _primaryWeaponButton;
    [SerializeField] private Button _secondaryWeaponButton;
    [SerializeField] private Button _utilityButton;

    [SerializeField] private string _defaultPrimaryAnimState = "infantry_combat_idle";
    [SerializeField] private string _defaultSecondaryAnimState = "handgun_combat_idle";

    [SerializeField] private GameObject _floatingTooltipPanel;
    [SerializeField] private TMP_Text _floatingTitleText;
    [SerializeField] private TMP_Text _floatingDescriptionText;
    [SerializeField] private Image _floatingIconImage;

    [SerializeField] private RoleButtonUI[] _roleButtons;
    [SerializeField] private float _radialRadius = 300f;
    [SerializeField] private float _angleSpacing = 25f;
    [SerializeField] private float _scaleFalloff = 0.2f;
    [SerializeField] private float _animationSpeed = 10f;
    [SerializeField] private Color _selectedRoleColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color _unselectedRoleColor = Color.white;

    [SerializeField] private RolePreviewModel[] _roleModels;

    [SerializeField] private Button _lockInButton;
    [SerializeField] private Button _confirmButton;

    private Team _assignedTeam = Team.Red;
    private PlayerRoleType _selectedRole = PlayerRoleType.Assault;
    private int _selectedRoleIndex = 0;
    private bool _hasLockedIn = false;
    private float _targetHealth;
    private float _targetArmor;
    private float _targetSpeed;
    private GameObject _currentEquippedWeaponInstance;

    public bool hasLockedIn => _hasLockedIn;
    public bool isRoleSelectionActive => !_hasLockedIn && _screenRoot != null && _screenRoot.activeSelf;
    public PlayerRoleType SelectedRole => _selectedRole;
    public Team SelectedTeam => _assignedTeam;
    public RoleDatabaseSO RoleDatabase => _roleDatabase;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (_floatingTooltipPanel != null)
        {
            _floatingTooltipPanel.SetActive(false);
        }

        BindButtonCallbacks();
        SetupEquipmentTriggers();
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
        if (_hasLockedIn) return;

        bool canLockIn = NetworkManager.Singleton == null || NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening;

        if (_confirmButton != null && _confirmButton.interactable != canLockIn) _confirmButton.interactable = canLockIn;
        if (_lockInButton != null && _lockInButton.interactable != canLockIn) _lockInButton.interactable = canLockIn;
    }

    private void AnimateRadialMenu()
    {
        if (_roleButtons == null || _roleButtons.Length == 0) return;

        for (int i = 0; i < _roleButtons.Length; i++)
        {
            RoleButtonUI btn = _roleButtons[i];
            if (btn == null || btn.RectTransform == null) continue;

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
        if (_confirmButton != null) _confirmButton.onClick.AddListener(ConfirmSelection);
        if (_lockInButton != null) _lockInButton.onClick.AddListener(ConfirmSelection);

        if (_roleButtons != null)
        {
            for (int i = 0; i < _roleButtons.Length; i++)
            {
                if (_roleButtons[i] == null || _roleButtons[i].RoleButton == null) continue;

                int index = i;
                _roleButtons[i].RoleButton.onClick.AddListener(() =>
                {
                    OnRoleClicked(index);
                });
            }
        }

        if (_primaryWeaponButton != null)
        {
            _primaryWeaponButton.onClick.AddListener(() => EquipWeaponOnPreviewSoldier(true));
        }

        if (_secondaryWeaponButton != null)
        {
            _secondaryWeaponButton.onClick.AddListener(() => EquipWeaponOnPreviewSoldier(false));
        }
    }

    private void SetupEquipmentTriggers()
    {
        SetupSingleEquipmentTrigger(_primaryWeaponButton, 0, () => EquipWeaponOnPreviewSoldier(true));
        SetupSingleEquipmentTrigger(_secondaryWeaponButton, 1, () => EquipWeaponOnPreviewSoldier(false));
        SetupSingleEquipmentTrigger(_utilityButton, 2, null);
    }

    private void SetupSingleEquipmentTrigger(Button btn, int index, System.Action onAction)
    {
        if (btn == null) return;

        AddTrigger(btn.gameObject, EventTriggerType.PointerEnter, () =>
        {
            ShowEquipmentTooltip(index);
            onAction?.Invoke();
        });
        AddTrigger(btn.gameObject, EventTriggerType.PointerExit, HideEquipmentTooltip);

        AddTrigger(btn.gameObject, EventTriggerType.Select, () =>
        {
            ShowEquipmentTooltip(index);
            onAction?.Invoke();
        });
        AddTrigger(btn.gameObject, EventTriggerType.Deselect, HideEquipmentTooltip);
    }

    private void AddTrigger(GameObject target, EventTriggerType type, System.Action action)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((_) => action?.Invoke());
        trigger.triggers.Add(entry);
    }

    private void OnRoleClicked(int index)
    {
        if (_hasLockedIn) return;
        _selectedRoleIndex = index;
        SelectRolePreview(_roleButtons[index].RoleType);
    }

    private void ShowEquipmentTooltip(int equipmentIndex)
    {
        if (_floatingTooltipPanel == null || _roleDatabase == null) return;

        RoleDataSO roleData = _roleDatabase.GetRole(_selectedRole);
        if (roleData == null) return;

        string title = string.Empty;
        string description = string.Empty;
        Sprite icon = null;

        switch (equipmentIndex)
        {
            case 0:
                title = roleData.primaryWeaponName;
                description = roleData.primaryWeaponDescription;
                icon = roleData.primaryWeaponIcon;
                break;
            case 1:
                title = roleData.secondaryWeaponName;
                description = roleData.secondaryWeaponDescription;
                icon = roleData.secondaryWeaponIcon;
                break;
            case 2:
                title = roleData.utilityName;
                description = roleData.utilityDescription;
                icon = roleData.utilityIcon;
                break;
        }

        if (_floatingTitleText != null) _floatingTitleText.text = title;
        if (_floatingDescriptionText != null) _floatingDescriptionText.text = description;

        if (_floatingIconImage != null)
        {
            if (icon != null)
            {
                _floatingIconImage.sprite = icon;
                _floatingIconImage.enabled = true;
            }
            else
            {
                _floatingIconImage.enabled = false;
            }
        }

        _floatingTooltipPanel.SetActive(true);
        _floatingTooltipPanel.transform.SetAsLastSibling();
    }

    private void HideEquipmentTooltip()
    {
        if (_floatingTooltipPanel != null)
        {
            _floatingTooltipPanel.SetActive(false);
        }
    }

    public void SelectRolePreview(PlayerRoleType role)
    {
        if (_hasLockedIn) return;
        _selectedRole = role;

        HideEquipmentTooltip();
        Update3DModel(role);

        if (_roleDatabase == null) return;
        RoleDataSO roleData = _roleDatabase.GetRole(role);
        if (roleData == null) return;

        if (_roleNameText != null) _roleNameText.text = roleData.roleName;
        if (_roleDescriptionText != null) _roleDescriptionText.text = roleData.roleDescription;
        if (_roleHealthText != null) _roleHealthText.text = string.Concat(HEALTH_PREFIX, roleData.maxHealth);
        if (_roleArmorText != null) _roleArmorText.text = string.Concat(ARMOR_PREFIX, roleData.maxArmor);
        if (_roleSpeedText != null) _roleSpeedText.text = string.Concat(SPEED_PREFIX, roleData.walkSpeed.ToString("F1"));

        if (_healthSlider != null) _healthSlider.maxValue = Mathf.Max(150f, roleData.maxHealth);
        if (_armorSlider != null) _armorSlider.maxValue = 100f;
        if (_speedSlider != null) _speedSlider.maxValue = 7f;

        _targetHealth = roleData.maxHealth;
        _targetArmor = roleData.maxArmor;
        _targetSpeed = roleData.walkSpeed;

        EquipWeaponOnPreviewSoldier(true);
    }

    private void Update3DModel(PlayerRoleType role)
    {
        if (_roleModels == null) return;

        foreach (var modelData in _roleModels)
        {
            if (modelData != null && modelData.ModelRoot != null)
            {
                modelData.ModelRoot.SetActive(modelData.RoleType == role);
            }
        }
    }

    public void EquipWeaponOnPreviewSoldier(bool isPrimary)
    {
        if (_roleDatabase == null) return;
        RoleDataSO roleData = _roleDatabase.GetRole(_selectedRole);
        if (roleData == null) return;

        GameObject weaponPrefab = isPrimary ? roleData.tpPrimaryWeaponPrefab : roleData.tpSecondaryWeaponPrefab;
        if (weaponPrefab == null) return;

        RolePreviewModel currentRoleModel = null;
        if (_roleModels != null)
        {
            for (int i = 0; i < _roleModels.Length; i++)
            {
                if (_roleModels[i] != null && _roleModels[i].RoleType == _selectedRole && _roleModels[i].ModelRoot != null)
                {
                    currentRoleModel = _roleModels[i];
                    break;
                }
            }
        }

        if (currentRoleModel == null || currentRoleModel.ModelRoot == null) return;

        Transform handSocket = currentRoleModel.HandSocketOverride;
        if (handSocket == null)
        {
            handSocket = AutoFindHandSocket(currentRoleModel.ModelRoot.transform);
        }

        if (handSocket == null) handSocket = currentRoleModel.ModelRoot.transform;

        for (int i = handSocket.childCount - 1; i >= 0; i--)
        {
            Transform child = handSocket.GetChild(i);
            if (_currentEquippedWeaponInstance != null && child == _currentEquippedWeaponInstance.transform)
            {
                Destroy(child.gameObject);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }

        if (_currentEquippedWeaponInstance != null)
        {
            Destroy(_currentEquippedWeaponInstance);
            _currentEquippedWeaponInstance = null;
        }

        _currentEquippedWeaponInstance = Instantiate(weaponPrefab, handSocket);

        Vector3 posOffset = isPrimary ? roleData.primaryPositionOffset : roleData.secondaryPositionOffset;
        Vector3 rotOffset = isPrimary ? roleData.primaryRotationOffset : roleData.secondaryRotationOffset;

        _currentEquippedWeaponInstance.transform.localPosition = posOffset;
        _currentEquippedWeaponInstance.transform.localRotation = Quaternion.Euler(rotOffset);
        _currentEquippedWeaponInstance.transform.localScale = weaponPrefab.transform.localScale;

        Collider[] cols = _currentEquippedWeaponInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        WeaponBase weaponScript = _currentEquippedWeaponInstance.GetComponentInChildren<WeaponBase>();
        if (weaponScript != null) weaponScript.enabled = false;

        string targetAnim = isPrimary ? roleData.primaryAnimState : roleData.secondaryAnimState;
        if (string.IsNullOrEmpty(targetAnim))
        {
            targetAnim = isPrimary ? _defaultPrimaryAnimState : _defaultSecondaryAnimState;
        }

        Animator anim = currentRoleModel.ModelRoot.GetComponentInChildren<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(targetAnim))
        {
            int animHash = Animator.StringToHash(targetAnim);
            if (anim.HasState(0, animHash))
            {
                anim.CrossFadeInFixedTime(targetAnim, 0.2f);
            }
        }
    }

    private Transform AutoFindHandSocket(Transform root)
    {
        string[] priorityNames = new string[]
        {
            "WeaponContainer",
            "WeaponContainer_R",
            "Bip001 R Hand",
            "Bip01 R Hand",
            "RightHand",
            "R_Hand",
            "Hand_R"
        };

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
        for (int p = 0; p < priorityNames.Length; p++)
        {
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i].name.Equals(priorityNames[p], System.StringComparison.OrdinalIgnoreCase) ||
                    allTransforms[i].name.IndexOf(priorityNames[p], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return allTransforms[i];
                }
            }
        }
        return null;
    }

    public void ConfirmSelection()
    {
        if (_hasLockedIn) return;

        _hasLockedIn = true;
        SetLockInText(_confirmButton);
        SetLockInText(_lockInButton);

        if (NetworkManager.Singleton != null)
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            CustomLobbyManager.SavePlayerData(localClientId, _assignedTeam, _selectedRole);

            if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
            {
                var controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
                if (controller != null) controller.RequestSelectRoleServerRpc(_selectedRole);

                var playerTeam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerTeam>();
                if (playerTeam != null && NetworkManager.Singleton.IsServer)
                {
                    playerTeam.CurrentTeam.Value = _assignedTeam;
                }
            }

            if (RoleLobbyManager.Instance != null && RoleLobbyManager.Instance.IsSpawned)
            {
                RoleLobbyManager.Instance.ConfirmRoleSelection(_selectedRole, _assignedTeam);
            }
        }

        CloseRoleScreen();
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CustomLobbyManager.Instance != null && NetworkManager.Singleton != null)
        {
            Team teamFromLobby = CustomLobbyManager.Instance.GetPlayerAssignedTeam(NetworkManager.Singleton.LocalClientId);
            if (teamFromLobby != Team.Neutral) _assignedTeam = teamFromLobby;
        }

        if (_roleButtons != null)
        {
            for (int i = 0; i < _roleButtons.Length; i++)
            {
                if (_roleButtons[i] != null && _roleButtons[i].RoleType == _selectedRole)
                {
                    _selectedRoleIndex = i;
                    break;
                }
            }
        }

        HideEquipmentTooltip();
        SelectRolePreview(_selectedRole);
        SnapSliders();
    }

    public void CloseRoleScreen()
    {
        HideEquipmentTooltip();
        if (_screenRoot != null) _screenRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandlePhaseChanged(RoundPhase previousPhase, RoundPhase currentPhase)
    {
        if (_hasLockedIn && (currentPhase == RoundPhase.Warmup || currentPhase == RoundPhase.InProgress))
        {
            CloseRoleScreen();
        }
    }
}