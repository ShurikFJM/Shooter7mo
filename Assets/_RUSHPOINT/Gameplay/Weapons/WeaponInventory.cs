using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : NetworkBehaviour
{
    private const int TOTAL_SLOTS = 3;
    private const float IMPACT_PARTICLE_LIFETIME = 0.6f;
    private const float TRACER_ANIMATION_DURATION = 0.04f;
    private const float TRACER_DISPLAY_HOLD_DURATION = 0.04f;

    [SerializeField] private Transform _firstPersonWeaponHolder;
    [SerializeField] private Transform _tpPrimaryHolder;
    [SerializeField] private Transform _tpSecondaryHolder;
    [SerializeField] private GameObject _defaultMeleePrefab;

    private readonly NetworkVariable<int> _activeSlotNetworked = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private readonly GameObject[] _weaponInstances = new GameObject[TOTAL_SLOTS];
    private readonly WeaponBase[] _equippedWeapons = new WeaponBase[TOTAL_SLOTS];
    private int _currentSlotIndex = 1;
    private bool _hasEquippedLoadout;

    public NetworkVariable<int> ActiveSlotNetworked => _activeSlotNetworked;
    public NetworkVariable<int> activeSlotNetworked => _activeSlotNetworked;

    public WeaponBase ActiveWeapon
    {
        get
        {
            int slotArrayIndex = _currentSlotIndex - 1;
            if (slotArrayIndex >= 0 && slotArrayIndex < _equippedWeapons.Length && _equippedWeapons[slotArrayIndex] != null)
            {
                return _equippedWeapons[slotArrayIndex];
            }

            WeaponBase[] childWeapons = GetComponentsInChildren<WeaponBase>(false);
            if (childWeapons != null && childWeapons.Length > 0)
            {
                return childWeapons[0];
            }

            return null;
        }
    }

    public int ActiveSlotIndex => _currentSlotIndex;

    private void Awake()
    {
        DisableFloatingCameraHolder();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _activeSlotNetworked.OnValueChanged += HandleActiveSlotChanged;
        DisableFloatingCameraHolder();

        if (!IsOwner)
        {
            UpdateEquippedSlotVisibility(_activeSlotNetworked.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _activeSlotNetworked.OnValueChanged -= HandleActiveSlotChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        HandleSlotInput();
    }

    private void DisableFloatingCameraHolder()
    {
        if (_firstPersonWeaponHolder == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera != null)
            {
                Transform foundHolder = playerCamera.transform.Find("WeaponHolder");
                if (foundHolder != null) _firstPersonWeaponHolder = foundHolder;
            }
        }

        if (_firstPersonWeaponHolder != null)
        {
            for (int i = _firstPersonWeaponHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_firstPersonWeaponHolder.GetChild(i).gameObject);
            }
            _firstPersonWeaponHolder.gameObject.SetActive(false);
        }
    }

    private Transform GetActiveHandSocket()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(false);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i].gameObject.activeInHierarchy && animators[i].isHuman)
            {
                Transform handBone = animators[i].GetBoneTransform(HumanBodyBones.RightHand);
                if (handBone != null) return handBone;
            }
        }

        string[] socketNames = new string[] {
            "WeaponContainer", "WeaponContainer_R", "mixamorig:RightHand",
            "RightHand", "Bip001 R Hand", "Bip01 R Hand", "hand_r", "hand.r"
        };
        Transform[] allActiveTransforms = GetComponentsInChildren<Transform>(false);

        for (int s = 0; s < socketNames.Length; s++)
        {
            for (int i = 0; i < allActiveTransforms.Length; i++)
            {
                if (allActiveTransforms[i].name.Equals(socketNames[s], System.StringComparison.OrdinalIgnoreCase))
                {
                    return allActiveTransforms[i];
                }
            }
        }

        if (_tpPrimaryHolder != null && _tpPrimaryHolder.gameObject.activeInHierarchy)
        {
            return _tpPrimaryHolder;
        }

        return transform;
    }

    public void SetupLoadoutForRole(GameObject primaryPrefab, GameObject secondaryPrefab, GameObject tpPrimaryPrefab, GameObject tpSecondaryPrefab)
    {
        DisableFloatingCameraHolder();
        ClearAllWeapons();

        Transform handSocket = GetActiveHandSocket();

        NetworkPlayerController pc = GetComponent<NetworkPlayerController>();
        if (pc == null) pc = GetComponentInParent<NetworkPlayerController>();

        GameObject resolvedPrimary = ResolveHandWeaponPrefab(tpPrimaryPrefab, primaryPrefab);
        GameObject resolvedSecondary = ResolveHandWeaponPrefab(tpSecondaryPrefab, secondaryPrefab);

        if (resolvedPrimary != null)
        {
            Vector3 pos = resolvedPrimary.transform.localPosition;
            Quaternion rot = resolvedPrimary.transform.localRotation;
            if (pc != null && pc.ActiveRole != null && (pc.ActiveRole.primaryPositionOffset != Vector3.zero || pc.ActiveRole.primaryRotationOffset != Vector3.zero))
            {
                pos = pc.ActiveRole.primaryPositionOffset;
                rot = Quaternion.Euler(pc.ActiveRole.primaryRotationOffset);
            }
            InstantiateWeaponInHand(0, resolvedPrimary, handSocket, pos, rot);
        }

        if (resolvedSecondary != null)
        {
            Vector3 pos = resolvedSecondary.transform.localPosition;
            Quaternion rot = resolvedSecondary.transform.localRotation;
            if (pc != null && pc.ActiveRole != null && (pc.ActiveRole.secondaryPositionOffset != Vector3.zero || pc.ActiveRole.secondaryRotationOffset != Vector3.zero))
            {
                pos = pc.ActiveRole.secondaryPositionOffset;
                rot = Quaternion.Euler(pc.ActiveRole.secondaryRotationOffset);
            }
            InstantiateWeaponInHand(1, resolvedSecondary, handSocket, pos, rot);
        }

        if (_defaultMeleePrefab != null)
        {
            InstantiateWeaponInHand(2, _defaultMeleePrefab, handSocket, _defaultMeleePrefab.transform.localPosition, _defaultMeleePrefab.transform.localRotation);
        }

        _hasEquippedLoadout = true;
        _currentSlotIndex = 1;

        int slotToShow = IsOwner ? 1 : _activeSlotNetworked.Value;
        UpdateEquippedSlotVisibility(slotToShow);

        if (IsOwner && IsSpawned)
        {
            _activeSlotNetworked.Value = 1;
        }
    }

    private GameObject ResolveHandWeaponPrefab(GameObject tpPrefab, GameObject fpPrefab)
    {
        if (tpPrefab != null)
        {
            WeaponBase wb = tpPrefab.GetComponent<WeaponBase>();
            if (wb == null) wb = tpPrefab.GetComponentInChildren<WeaponBase>(true);
            if (wb != null) return tpPrefab;
        }

        return fpPrefab != null ? fpPrefab : tpPrefab;
    }

    private void InstantiateWeaponInHand(int slotArrayIndex, GameObject weaponPrefab, Transform handSocket, Vector3 localPos, Quaternion localRot)
    {
        if (handSocket == null || weaponPrefab == null) return;

        GameObject weaponInstance = Instantiate(weaponPrefab, handSocket, false);

        weaponInstance.transform.localPosition = localPos;
        weaponInstance.transform.localRotation = localRot;
        weaponInstance.transform.localScale = weaponPrefab.transform.localScale;

        WeaponBase weaponBase = weaponInstance.GetComponent<WeaponBase>();
        if (weaponBase == null) weaponBase = weaponInstance.GetComponentInChildren<WeaponBase>(true);

        if (weaponBase != null)
        {
            weaponBase.enabled = true;
            weaponBase.UpdateDefaultTransform(localPos, localRot);
            weaponBase.ResetAmmo();
            weaponBase.EnsureFirePoint();
        }

        _weaponInstances[slotArrayIndex] = weaponInstance;
        _equippedWeapons[slotArrayIndex] = weaponBase;
        weaponInstance.SetActive(false);
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

        if (_tpPrimaryHolder != null)
        {
            for (int i = _tpPrimaryHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_tpPrimaryHolder.GetChild(i).gameObject);
            }
        }

        if (_tpSecondaryHolder != null)
        {
            for (int i = _tpSecondaryHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_tpSecondaryHolder.GetChild(i).gameObject);
            }
        }
    }

    private void HandleSlotInput()
    {
        if (!_hasEquippedLoadout) return;

        if (InputManager.Instance != null)
        {
            if (InputManager.Instance.WasSlot1Triggered) SwitchSlot(1);
            else if (InputManager.Instance.WasSlot2Triggered) SwitchSlot(2);
            else if (InputManager.Instance.WasSlot3Triggered) SwitchSlot(3);
        }
        else if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchSlot(1);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchSlot(2);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchSlot(3);
        }

        if (Mouse.current != null)
        {
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (scrollDelta > 0f) CycleSlot(-1);
            else if (scrollDelta < 0f) CycleSlot(1);
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
        UpdateEquippedSlotVisibility(_currentSlotIndex);

        if (IsSpawned && IsOwner)
        {
            _activeSlotNetworked.Value = _currentSlotIndex;
        }
    }

    private void HandleActiveSlotChanged(int previousSlot, int currentSlot)
    {
        _currentSlotIndex = currentSlot;
        UpdateEquippedSlotVisibility(currentSlot);
    }

    private void UpdateEquippedSlotVisibility(int activeSlot)
    {
        for (int i = 0; i < _weaponInstances.Length; i++)
        {
            if (_weaponInstances[i] != null)
            {
                _weaponInstances[i].SetActive(i == (activeSlot - 1));
            }
        }
    }

    public void ResetAllWeaponsAmmo()
    {
        for (int i = 0; i < _equippedWeapons.Length; i++)
        {
            if (_equippedWeapons[i] != null) _equippedWeapons[i].ResetAmmo();
        }

        WeaponBase[] childWeapons = GetComponentsInChildren<WeaponBase>(true);
        for (int i = 0; i < childWeapons.Length; i++)
        {
            if (childWeapons[i] != null) childWeapons[i].ResetAmmo();
        }
    }

    private WeaponData GetActiveWeaponDataSynced()
    {
        if (ActiveWeapon != null && ActiveWeapon.WeaponData != null)
        {
            return ActiveWeapon.WeaponData;
        }

        NetworkPlayerController playerController = GetComponent<NetworkPlayerController>();
        if (playerController == null) playerController = GetComponentInParent<NetworkPlayerController>();
        if (playerController == null || playerController.ActiveRole == null) return null;

        int activeSlot = _activeSlotNetworked.Value;
        GameObject weaponPrefab = null;

        if (activeSlot == 1) weaponPrefab = playerController.ActiveRole.primaryWeaponPrefab;
        else if (activeSlot == 2) weaponPrefab = playerController.ActiveRole.secondaryWeaponPrefab;
        else if (activeSlot == 3) weaponPrefab = _defaultMeleePrefab;

        if (weaponPrefab != null)
        {
            WeaponBase weaponBase = weaponPrefab.GetComponent<WeaponBase>();
            if (weaponBase == null) weaponBase = weaponPrefab.GetComponentInChildren<WeaponBase>(true);
            if (weaponBase != null) return weaponBase.WeaponData;
        }

        return null;
    }

    [Rpc(SendTo.Server)]
    public void RequestDealDamageServerRpc(NetworkObjectReference victimRef, HitboxType hitboxType)
    {
        if (!victimRef.TryGet(out NetworkObject victimObject)) return;

        NetworkHealth victimHealth = victimObject.GetComponent<NetworkHealth>();
        if (victimHealth == null) victimHealth = victimObject.GetComponentInChildren<NetworkHealth>();
        if (victimHealth == null) victimHealth = victimObject.GetComponentInParent<NetworkHealth>();
        if (victimHealth == null) return;

        WeaponData activeWeaponData = GetActiveWeaponDataSynced();
        float baseDamage = activeWeaponData != null ? activeWeaponData.damage : 25f;

        victimHealth.ApplyDamageServer(baseDamage, hitboxType, OwnerClientId);
    }

    [Rpc(SendTo.Server)]
    public void BroadcastShootServerRpc(Vector3 hitPoint)
    {
        BroadcastShootClientRpc(hitPoint, Vector3.up, (byte)SurfaceImpactType.None);
    }

    [Rpc(SendTo.Server)]
    public void BroadcastShootServerRpc(Vector3 hitPoint, Vector3 hitNormal, byte impactType)
    {
        BroadcastShootClientRpc(hitPoint, hitNormal, impactType);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastShootClientRpc(Vector3 hitPoint, Vector3 hitNormal, byte impactType)
    {
        if (IsOwner) return;

        WeaponData activeWeaponData = GetActiveWeaponDataSynced();

        if (activeWeaponData != null)
        {
            AudioClip[] audioClips = activeWeaponData.shootSounds;
            if (audioClips != null && audioClips.Length > 0)
            {
                AudioClip selectedClip = audioClips[Random.Range(0, audioClips.Length)];
                GameObject soundObject = new GameObject("NetworkShotAudio");
                soundObject.transform.position = transform.position + (Vector3.up * 1.5f);
                AudioSource audioSource = soundObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 3f;
                audioSource.maxDistance = 50f;
                audioSource.clip = selectedClip;
                audioSource.Play();
                Destroy(soundObject, selectedClip.length + 0.1f);
            }
        }

        if (activeWeaponData != null && PoolManager.Instance != null)
        {
            SurfaceImpactType surface = (SurfaceImpactType)impactType;
            if (surface == SurfaceImpactType.Wall && activeWeaponData.wallImpactVfxPrefab != null)
            {
                Quaternion impactRotation = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
                PoolManager.Instance.SpawnImpact(activeWeaponData.wallImpactVfxPrefab, hitPoint + (hitNormal * 0.02f), impactRotation, IMPACT_PARTICLE_LIFETIME);
            }
            else if (surface == SurfaceImpactType.Player && activeWeaponData.playerImpactVfxPrefab != null)
            {
                Quaternion impactRotation = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
                PoolManager.Instance.SpawnImpact(activeWeaponData.playerImpactVfxPrefab, hitPoint, impactRotation, IMPACT_PARTICLE_LIFETIME);
            }
        }

        Vector3 tracerStart = transform.position + (Vector3.up * 1.4f);
        WeaponBase remoteWeapon = ActiveWeapon;
        if (remoteWeapon != null)
        {
            tracerStart = remoteWeapon.GetMuzzleWorldPosition();
        }

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.StartCoroutine(RenderNetworkTracerRoutine(tracerStart, hitPoint));
        }
    }

    private static IEnumerator RenderNetworkTracerRoutine(Vector3 startPoint, Vector3 endPoint)
    {
        if (PoolManager.Instance == null) yield break;

        LineRenderer lineRenderer = PoolManager.Instance.GetTracer(null);
        if (lineRenderer == null) yield break;

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, startPoint);

        float elapsedTime = 0f;
        while (elapsedTime < TRACER_ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(startPoint, endPoint, elapsedTime / TRACER_ANIMATION_DURATION);
            if (lineRenderer != null) lineRenderer.SetPosition(1, currentPos);
            yield return null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(1, endPoint);
            yield return new WaitForSeconds(TRACER_DISPLAY_HOLD_DURATION);
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
            PoolManager.Instance.ReturnTracer(lineRenderer);
        }
    }
}