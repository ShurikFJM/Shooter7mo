using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : NetworkBehaviour
{
    private const int TOTAL_SLOTS = 3;

    [SerializeField] private Transform _firstPersonWeaponHolder;
    [SerializeField] private Transform _tpPrimaryHolder;
    [SerializeField] private Transform _tpSecondaryHolder;
    [SerializeField] private GameObject _defaultMeleePrefab;

    public NetworkVariable<int> activeSlotNetworked = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private readonly GameObject[] _weaponInstances = new GameObject[TOTAL_SLOTS];
    private readonly WeaponBase[] _equippedWeapons = new WeaponBase[TOTAL_SLOTS];
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
        activeSlotNetworked.OnValueChanged += HandleActiveSlotChanged;

        if (IsOwner) SwitchSlot(activeSlotNetworked.Value);
        else UpdateThirdPersonWeaponVisibility(activeSlotNetworked.Value);
    }

    public override void OnNetworkDespawn()
    {
        activeSlotNetworked.OnValueChanged -= HandleActiveSlotChanged;
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
                if (foundHolder != null) _firstPersonWeaponHolder = foundHolder;
            }
        }
    }

    public void SetupLoadoutForRole(GameObject primaryPrefab, GameObject secondaryPrefab, GameObject tpPrimaryPrefab, GameObject tpSecondaryPrefab)
    {
        EnsureHolders();
        ClearAllWeapons();

        if (IsOwner)
        {
            if (primaryPrefab != null) InstantiateWeaponSlot(0, primaryPrefab);
            if (secondaryPrefab != null) InstantiateWeaponSlot(1, secondaryPrefab);
            if (_defaultMeleePrefab != null) InstantiateWeaponSlot(2, _defaultMeleePrefab);
        }

        if (tpPrimaryPrefab != null && _tpPrimaryHolder != null)
        {
            _currentTpPrimary = Instantiate(tpPrimaryPrefab, _tpPrimaryHolder);
            _currentTpPrimary.transform.localPosition = tpPrimaryPrefab.transform.localPosition;
            _currentTpPrimary.transform.localRotation = tpPrimaryPrefab.transform.localRotation;
            _currentTpPrimary.transform.localScale = tpPrimaryPrefab.transform.localScale;
        }

        if (tpSecondaryPrefab != null && _tpSecondaryHolder != null)
        {
            _currentTpSecondary = Instantiate(tpSecondaryPrefab, _tpSecondaryHolder);
            _currentTpSecondary.transform.localPosition = tpSecondaryPrefab.transform.localPosition;
            _currentTpSecondary.transform.localRotation = tpSecondaryPrefab.transform.localRotation;
            _currentTpSecondary.transform.localScale = tpSecondaryPrefab.transform.localScale;
        }

        _hasEquippedLoadout = true;
        _currentSlotIndex = 1;

        if (IsOwner)
        {
            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                if (_weaponInstances[i] != null) _weaponInstances[i].SetActive(i == 0);
            }
            if (IsSpawned) activeSlotNetworked.Value = 1;
        }

        UpdateThirdPersonWeaponVisibility(activeSlotNetworked.Value);
    }

    private void InstantiateWeaponSlot(int slotArrayIndex, GameObject weaponPrefab)
    {
        if (_firstPersonWeaponHolder == null || weaponPrefab == null) return;

        GameObject weaponObj = Instantiate(weaponPrefab, _firstPersonWeaponHolder, false);

        weaponObj.transform.localPosition = weaponPrefab.transform.localPosition;
        weaponObj.transform.localRotation = weaponPrefab.transform.localRotation;
        weaponObj.transform.localScale = weaponPrefab.transform.localScale;

        WeaponBase weaponBase = weaponObj.GetComponent<WeaponBase>();
        if (weaponBase == null) weaponBase = weaponObj.GetComponentInChildren<WeaponBase>(true);

        if (weaponBase != null)
        {
            weaponBase.UpdateDefaultTransform(weaponPrefab.transform.localPosition, weaponPrefab.transform.localRotation);
        }

        _weaponInstances[slotArrayIndex] = weaponObj;
        _equippedWeapons[slotArrayIndex] = weaponBase;
        weaponObj.SetActive(false);
    }

    private void ClearAllWeapons()
    {
        for (int i = 0; i < TOTAL_SLOTS; i++)
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
        if (!_hasEquippedLoadout || Keyboard.current == null) return;

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
        if (newSlot > TOTAL_SLOTS) newSlot = 1;
        if (newSlot < 1) newSlot = TOTAL_SLOTS;
        SwitchSlot(newSlot);
    }

    public void SwitchSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > TOTAL_SLOTS) return;

        _currentSlotIndex = slotIndex;

        for (int i = 0; i < _weaponInstances.Length; i++)
        {
            if (_weaponInstances[i] != null)
            {
                _weaponInstances[i].SetActive(i == (_currentSlotIndex - 1));
            }
        }

        if (IsSpawned && IsOwner) activeSlotNetworked.Value = _currentSlotIndex;
        UpdateThirdPersonWeaponVisibility(_currentSlotIndex);
    }

    private void HandleActiveSlotChanged(int previousSlot, int currentSlot)
    {
        UpdateThirdPersonWeaponVisibility(currentSlot);
    }

    private void UpdateThirdPersonWeaponVisibility(int activeSlot)
    {
        bool showTp = !IsOwner;
        if (_currentTpPrimary != null) _currentTpPrimary.SetActive(showTp && activeSlot == 1);
        if (_currentTpSecondary != null) _currentTpSecondary.SetActive(showTp && activeSlot == 2);
    }

    public void ResetAllWeaponsAmmo()
    {
        for (int i = 0; i < _equippedWeapons.Length; i++)
        {
            if (_equippedWeapons[i] != null) _equippedWeapons[i].ResetAmmo();
        }
    }

    private WeaponData GetActiveWeaponDataSynced()
    {
        NetworkPlayerController npc = GetComponent<NetworkPlayerController>();
        if (npc == null || npc.ActiveRole == null) return null;

        int slot = activeSlotNetworked.Value;
        GameObject prefab = null;

        if (slot == 1) prefab = npc.ActiveRole.primaryWeaponPrefab;
        else if (slot == 2) prefab = npc.ActiveRole.secondaryWeaponPrefab;
        else if (slot == 3) prefab = _defaultMeleePrefab;

        if (prefab != null)
        {
            WeaponBase wb = prefab.GetComponent<WeaponBase>();
            if (wb != null) return wb.WeaponData;
        }
        return null;
    }

    [Rpc(SendTo.Server)]
    public void RequestDealDamageServerRpc(NetworkObjectReference victimRef, HitboxType hitboxType)
    {
        if (!victimRef.TryGet(out NetworkObject victimObj)) return;

        NetworkHealth victimHealth = victimObj.GetComponent<NetworkHealth>();
        if (victimHealth == null) return;

        WeaponData activeData = GetActiveWeaponDataSynced();
        if (activeData == null) return;

        float baseDamage = activeData.damage;
        float finalDamage = baseDamage;

        if (hitboxType == HitboxType.Head) finalDamage *= 2.0f;
        else if (hitboxType == HitboxType.Legs) finalDamage *= 0.75f;

        victimHealth.ApplyDamageServer(finalDamage, hitboxType, OwnerClientId);
    }

    [Rpc(SendTo.Server)]
    public void BroadcastShootServerRpc(Vector3 hitPoint)
    {
        BroadcastShootClientRpc(hitPoint);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastShootClientRpc(Vector3 hitPoint)
    {
        if (IsOwner) return;

        WeaponData activeData = GetActiveWeaponDataSynced();
        if (activeData == null) return;

        AudioClip[] sounds = activeData.shootSounds;
        if (sounds != null && sounds.Length > 0)
        {
            AudioClip clip = sounds[Random.Range(0, sounds.Length)];
            GameObject soundObj = new GameObject("NetworkShotAudio");
            soundObj.transform.position = transform.position + (Vector3.up * 1.5f);
            AudioSource src = soundObj.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.minDistance = 3f;
            src.maxDistance = 50f;
            src.clip = clip;
            src.Play();
            Destroy(soundObj, clip.length + 0.1f);
        }

        int slot = activeSlotNetworked.Value;
        Transform tpWeapon = slot == 1 ? (_currentTpPrimary != null ? _currentTpPrimary.transform : null) :
                             slot == 2 ? (_currentTpSecondary != null ? _currentTpSecondary.transform : null) : null;

        Vector3 tracerStart = tpWeapon != null ? tpWeapon.position : (transform.position + Vector3.up * 1.5f);
        StartCoroutine(RenderNetworkTracer(tracerStart, hitPoint));
    }

    private IEnumerator RenderNetworkTracer(Vector3 start, Vector3 end)
    {
        if (PoolManager.Instance == null) yield break;

        LineRenderer line = PoolManager.Instance.GetTracer(null);
        line.SetPosition(0, start);
        line.SetPosition(1, start);

        float elapsedTime = 0f;
        while (elapsedTime < 0.04f)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / 0.04f);
            line.SetPosition(1, currentPos);
            yield return null;
        }

        line.SetPosition(1, end);
        yield return new WaitForSeconds(0.05f);
        PoolManager.Instance.ReturnTracer(line);
    }
}