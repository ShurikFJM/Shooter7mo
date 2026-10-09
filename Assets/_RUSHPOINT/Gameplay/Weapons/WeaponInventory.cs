using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : NetworkBehaviour
{
    private const int _TOTAL_SLOTS = 3;

    [SerializeField] private Transform _firstPersonWeaponHolder;
    [SerializeField] private Transform _tpPrimaryHolder;
    [SerializeField] private Transform _tpSecondaryHolder;
    [SerializeField] private GameObject _defaultMeleePrefab;

    public NetworkVariable<int> ActiveSlotNetworked = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private readonly GameObject[] _weaponInstances = new GameObject[_TOTAL_SLOTS];
    private readonly WeaponBase[] _equippedWeapons = new WeaponBase[_TOTAL_SLOTS];
    private GameObject _currentTpPrimary;
    private GameObject _currentTpSecondary;
    private int _currentSlotIndex = 1;
    private bool _hasEquippedLoadout = false;

    public WeaponBase ActiveWeapon => _equippedWeapons[_currentSlotIndex - 1];
    public int ActiveSlotIndex => _currentSlotIndex;

    private void Awake()
    {
        EnsureHolders();
    }

    public override void OnNetworkSpawn()
    {
        EnsureHolders();
        ActiveSlotNetworked.OnValueChanged += HandleActiveSlotChanged;

        if (IsOwner)
        {
            SwitchSlot(ActiveSlotNetworked.Value);
        }
        else
        {
            UpdateThirdPersonWeaponVisibility(ActiveSlotNetworked.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        ActiveSlotNetworked.OnValueChanged -= HandleActiveSlotChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleSlotInput();
    }

    private void EnsureHolders()
    {
        if (_firstPersonWeaponHolder == null)
        {
            Camera playerCam = GetComponentInChildren<Camera>(true);
            if (playerCam != null)
            {
                Transform foundHolder = playerCam.transform.Find("WeaponHolder");
                if (foundHolder != null)
                {
                    _firstPersonWeaponHolder = foundHolder;
                }
            }
        }
    }

    public void SetupLoadoutForRole(GameObject primaryPrefab, GameObject secondaryPrefab, GameObject tpPrimaryPrefab, GameObject tpSecondaryPrefab)
    {
        EnsureHolders();
        ClearAllWeapons();

        if (IsOwner)
        {
            if (primaryPrefab != null)
            {
                InstantiateWeaponSlot(0, primaryPrefab);
            }

            if (secondaryPrefab != null)
            {
                InstantiateWeaponSlot(1, secondaryPrefab);
            }

            if (_defaultMeleePrefab != null)
            {
                InstantiateWeaponSlot(2, _defaultMeleePrefab);
            }
        }

        if (tpPrimaryPrefab != null && _tpPrimaryHolder != null)
        {
            _currentTpPrimary = Instantiate(tpPrimaryPrefab, _tpPrimaryHolder);
            _currentTpPrimary.transform.localPosition = Vector3.zero;
            _currentTpPrimary.transform.localRotation = Quaternion.identity;
            _currentTpPrimary.transform.localScale = Vector3.one;
        }

        if (tpSecondaryPrefab != null && _tpSecondaryHolder != null)
        {
            _currentTpSecondary = Instantiate(tpSecondaryPrefab, _tpSecondaryHolder);
            _currentTpSecondary.transform.localPosition = Vector3.zero;
            _currentTpSecondary.transform.localRotation = Quaternion.identity;
            _currentTpSecondary.transform.localScale = Vector3.one;
        }

        _hasEquippedLoadout = true;
        _currentSlotIndex = 1;

        if (IsOwner)
        {
            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                if (_weaponInstances[i] != null)
                {
                    _weaponInstances[i].SetActive(i == 0);
                }
            }

            if (IsSpawned)
            {
                ActiveSlotNetworked.Value = 1;
            }
        }

        UpdateThirdPersonWeaponVisibility(ActiveSlotNetworked.Value);
    }

    private void InstantiateWeaponSlot(int slotArrayIndex, GameObject weaponPrefab)
    {
        if (_firstPersonWeaponHolder == null || weaponPrefab == null) return;

        GameObject weaponObj = Instantiate(weaponPrefab, _firstPersonWeaponHolder);

        weaponObj.transform.localPosition = weaponPrefab.transform.localPosition;
        weaponObj.transform.localRotation = weaponPrefab.transform.localRotation;
        weaponObj.transform.localScale = weaponPrefab.transform.localScale;

        WeaponBase weaponBase = weaponObj.GetComponent<WeaponBase>();
        if (weaponBase == null)
        {
            weaponBase = weaponObj.GetComponentInChildren<WeaponBase>(true);
        }

        _weaponInstances[slotArrayIndex] = weaponObj;
        _equippedWeapons[slotArrayIndex] = weaponBase;
        weaponObj.SetActive(false);
    }

    private void ClearAllWeapons()
    {
        for (int i = 0; i < _TOTAL_SLOTS; i++)
        {
            if (_weaponInstances[i] != null)
            {
                Destroy(_weaponInstances[i]);
                _weaponInstances[i] = null;
            }
            _equippedWeapons[i] = null;
        }

        if (_firstPersonWeaponHolder != null)
        {
            for (int i = _firstPersonWeaponHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_firstPersonWeaponHolder.GetChild(i).gameObject);
            }
        }

        if (_currentTpPrimary != null)
        {
            Destroy(_currentTpPrimary);
            _currentTpPrimary = null;
        }

        if (_currentTpSecondary != null)
        {
            Destroy(_currentTpSecondary);
            _currentTpSecondary = null;
        }
    }

    private void HandleSlotInput()
    {
        if (!_hasEquippedLoadout) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchSlot(1);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchSlot(2);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchSlot(3);

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f) CycleSlot(-1);
            else if (scroll < 0f) CycleSlot(1);
        }
    }

    private void CycleSlot(int direction)
    {
        int newSlot = _currentSlotIndex + direction;
        if (newSlot > _TOTAL_SLOTS) newSlot = 1;
        if (newSlot < 1) newSlot = _TOTAL_SLOTS;

        SwitchSlot(newSlot);
    }

    public void SwitchSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > _TOTAL_SLOTS) return;

        _currentSlotIndex = slotIndex;

        for (int i = 0; i < _weaponInstances.Length; i++)
        {
            if (_weaponInstances[i] != null)
            {
                _weaponInstances[i].SetActive(i == (_currentSlotIndex - 1));
            }
        }

        if (IsSpawned && IsOwner)
        {
            ActiveSlotNetworked.Value = _currentSlotIndex;
        }

        UpdateThirdPersonWeaponVisibility(_currentSlotIndex);
    }

    private void HandleActiveSlotChanged(int previousSlot, int currentSlot)
    {
        UpdateThirdPersonWeaponVisibility(currentSlot);
    }

    private void UpdateThirdPersonWeaponVisibility(int activeSlot)
    {
        bool showTp = !IsOwner;

        if (_currentTpPrimary != null)
        {
            _currentTpPrimary.SetActive(showTp && activeSlot == 1);
        }

        if (_currentTpSecondary != null)
        {
            _currentTpSecondary.SetActive(showTp && activeSlot == 2);
        }
    }

    public void ResetAllWeaponsAmmo()
    {
        for (int i = 0; i < _equippedWeapons.Length; i++)
        {
            if (_equippedWeapons[i] != null)
            {
                _equippedWeapons[i].ResetAmmo();
            }
        }
    }
}