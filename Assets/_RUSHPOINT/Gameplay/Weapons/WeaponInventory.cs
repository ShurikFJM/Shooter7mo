using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : NetworkBehaviour
{
    private const int _TOTAL_SLOTS = 3;
    private const float _IMPACT_PARTICLE_LIFETIME = 0.6f;
    private const float _TRACER_ANIMATION_DURATION = 0.04f;
    private const float _TRACER_DISPLAY_HOLD_DURATION = 0.04f;

    [Header("Holders")]
    [SerializeField] private Transform _tpPrimaryHolder;
    [SerializeField] private Transform _tpSecondaryHolder;
    [SerializeField] private GameObject _defaultMeleePrefab;

    private readonly NetworkVariable<int> _activeSlotNetworked = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private readonly GameObject[] _weaponInstances = new GameObject[_TOTAL_SLOTS];
    private readonly WeaponBase[] _equippedWeapons = new WeaponBase[_TOTAL_SLOTS];
    private GameObject _currentTpPrimary;
    private GameObject _currentTpSecondary;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _activeSlotNetworked.OnValueChanged += HandleActiveSlotChanged;

        if (IsOwner)
        {
            if (_equippedWeapons[0] != null)
            {
                _hasEquippedLoadout = true;
                _currentSlotIndex = 1;

                for (int i = 0; i < _weaponInstances.Length; i++)
                {
                    if (_weaponInstances[i] != null)
                    {
                        _weaponInstances[i].SetActive(i == 0);
                    }
                }
            }
        }
        else
        {
            UpdateThirdPersonWeaponVisibility(_activeSlotNetworked.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _activeSlotNetworked.OnValueChanged -= HandleActiveSlotChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleSlotInput();
    }

    private Transform GetActiveHandSocket()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
        {
            Transform handBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (handBone != null) return handBone;
        }

        // 2. Búsqueda por nombres estándar de jerarquía
        string[] socketNames = new string[] {
            "WeaponContainer", "WeaponContainer_R", "mixamorig:RightHand",
            "RightHand", "Bip001 R Hand", "Bip01 R Hand", "hand_r", "hand.r"
        };
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);

        for (int s = 0; s < socketNames.Length; s++)
        {
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i].name.Equals(socketNames[s], System.StringComparison.OrdinalIgnoreCase))
                {
                    return allTransforms[i];
                }
            }
        }

        for (int i = 0; i < allTransforms.Length; i++)
        {
            string tName = allTransforms[i].name;
            if (tName.IndexOf("RightHand", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                tName.IndexOf("WeaponContainer", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return allTransforms[i];
            }
        }

        return transform;
    }

    public void SetupLoadoutForRole(GameObject primaryPrefab, GameObject secondaryPrefab, GameObject tpPrimaryPrefab, GameObject tpSecondaryPrefab)
    {
        ClearAllWeapons();

        Transform handSocket = GetActiveHandSocket();
        Transform primarySocket = _tpPrimaryHolder != null ? _tpPrimaryHolder : handSocket;
        Transform secondarySocket = _tpSecondaryHolder != null ? _tpSecondaryHolder : handSocket;

        NetworkPlayerController pc = GetComponent<NetworkPlayerController>();
        if (pc == null) pc = GetComponentInParent<NetworkPlayerController>();

        Vector3 primOffsetPos = Vector3.zero;
        Quaternion primOffsetRot = Quaternion.identity;
        Vector3 secOffsetPos = Vector3.zero;
        Quaternion secOffsetRot = Quaternion.identity;

        if (pc != null && pc.ActiveRole != null)
        {
            if (pc.ActiveRole.primaryPositionOffset != Vector3.zero || pc.ActiveRole.primaryRotationOffset != Vector3.zero)
            {
                primOffsetPos = pc.ActiveRole.primaryPositionOffset;
                primOffsetRot = Quaternion.Euler(pc.ActiveRole.primaryRotationOffset);
            }
            if (pc.ActiveRole.secondaryPositionOffset != Vector3.zero || pc.ActiveRole.secondaryRotationOffset != Vector3.zero)
            {
                secOffsetPos = pc.ActiveRole.secondaryPositionOffset;
                secOffsetRot = Quaternion.Euler(pc.ActiveRole.secondaryRotationOffset);
            }
        }

        if (IsOwner)
        {
            // Para el jugador local: instanciamos las armas funcionales directamente en la mano
            if (primaryPrefab != null)
            {
                InstantiateFunctionalWeapon(0, primaryPrefab, primarySocket, primOffsetPos, primOffsetRot);
            }
            if (secondaryPrefab != null)
            {
                InstantiateFunctionalWeapon(1, secondaryPrefab, secondarySocket, secOffsetPos, secOffsetRot);
            }
            if (_defaultMeleePrefab != null)
            {
                InstantiateFunctionalWeapon(2, _defaultMeleePrefab, primarySocket, Vector3.zero, Quaternion.identity);
            }

            _hasEquippedLoadout = true;
            _currentSlotIndex = 1;

            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                if (_weaponInstances[i] != null) _weaponInstances[i].SetActive(i == 0);
            }

            if (IsSpawned) _activeSlotNetworked.Value = 1;
        }
        else
        {
            // Para jugadores remotos: instanciamos los modelos visuales en su mano
            if (tpPrimaryPrefab != null && primarySocket != null)
            {
                _currentTpPrimary = Instantiate(tpPrimaryPrefab, primarySocket, false);
                _currentTpPrimary.transform.localPosition = primOffsetPos;
                _currentTpPrimary.transform.localRotation = primOffsetRot;
                _currentTpPrimary.transform.localScale = tpPrimaryPrefab.transform.localScale;

                Collider[] cols = _currentTpPrimary.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

                WeaponBase wb = _currentTpPrimary.GetComponentInChildren<WeaponBase>(true);
                if (wb != null) wb.enabled = false;
            }

            if (tpSecondaryPrefab != null && secondarySocket != null)
            {
                _currentTpSecondary = Instantiate(tpSecondaryPrefab, secondarySocket, false);
                _currentTpSecondary.transform.localPosition = secOffsetPos;
                _currentTpSecondary.transform.localRotation = secOffsetRot;
                _currentTpSecondary.transform.localScale = tpSecondaryPrefab.transform.localScale;

                Collider[] cols = _currentTpSecondary.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

                WeaponBase wb = _currentTpSecondary.GetComponentInChildren<WeaponBase>(true);
                if (wb != null) wb.enabled = false;
            }

            UpdateThirdPersonWeaponVisibility(_activeSlotNetworked.Value);
        }
    }

    private void InstantiateFunctionalWeapon(int slotIndex, GameObject prefab, Transform socket, Vector3 localPos, Quaternion localRot)
    {
        if (prefab == null || socket == null) return;

        GameObject instance = Instantiate(prefab, socket, false);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
        instance.transform.localScale = prefab.transform.localScale;

        WeaponBase weaponBase = instance.GetComponent<WeaponBase>();
        if (weaponBase == null) weaponBase = instance.GetComponentInChildren<WeaponBase>(true);

        if (weaponBase != null)
        {
            weaponBase.UpdateDefaultTransform(localPos, localRot);
            weaponBase.ResetAmmo();
            weaponBase.EnsureFirePoint();
        }

        _weaponInstances[slotIndex] = instance;
        _equippedWeapons[slotIndex] = weaponBase;
        instance.SetActive(false);
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
        if (newSlot > _TOTAL_SLOTS) newSlot = 1;
        if (newSlot < 1) newSlot = _TOTAL_SLOTS;
        SwitchSlot(newSlot);
    }

    public void SwitchSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > _TOTAL_SLOTS) return;

        _currentSlotIndex = slotIndex;

        if (IsOwner)
        {
            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                if (_weaponInstances[i] != null)
                {
                    _weaponInstances[i].SetActive(i == (_currentSlotIndex - 1));
                }
            }

            if (IsSpawned) _activeSlotNetworked.Value = _currentSlotIndex;
        }
        else
        {
            UpdateThirdPersonWeaponVisibility(_currentSlotIndex);
        }
    }

    private void HandleActiveSlotChanged(int previousSlot, int currentSlot)
    {
        if (!IsOwner)
        {
            UpdateThirdPersonWeaponVisibility(currentSlot);
        }
    }

    private void UpdateThirdPersonWeaponVisibility(int activeSlot)
    {
        if (_currentTpPrimary != null)
            _currentTpPrimary.SetActive(activeSlot == 1);

        if (_currentTpSecondary != null)
            _currentTpSecondary.SetActive(activeSlot == 2);
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
        float baseDamage = 25f;

        if (activeWeaponData != null)
        {
            baseDamage = activeWeaponData.damage;
        }
        else if (ActiveWeapon != null && ActiveWeapon.WeaponData != null)
        {
            baseDamage = ActiveWeapon.WeaponData.damage;
        }

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
        if (activeWeaponData == null && ActiveWeapon != null) activeWeaponData = ActiveWeapon.WeaponData;
        if (activeWeaponData == null) return;

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

        if (PoolManager.Instance != null)
        {
            SurfaceImpactType surface = (SurfaceImpactType)impactType;
            if (surface == SurfaceImpactType.Wall && activeWeaponData.wallImpactVfxPrefab != null)
            {
                Quaternion impactRotation = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
                PoolManager.Instance.SpawnImpact(activeWeaponData.wallImpactVfxPrefab, hitPoint + (hitNormal * 0.02f), impactRotation, _IMPACT_PARTICLE_LIFETIME);
            }
            else if (surface == SurfaceImpactType.Player && activeWeaponData.playerImpactVfxPrefab != null)
            {
                Quaternion impactRotation = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
                PoolManager.Instance.SpawnImpact(activeWeaponData.playerImpactVfxPrefab, hitPoint, impactRotation, _IMPACT_PARTICLE_LIFETIME);
            }
        }

        int slot = _activeSlotNetworked.Value;
        Transform thirdPersonWeapon = slot == 1 ? (_currentTpPrimary != null ? _currentTpPrimary.transform : null) :
                                      slot == 2 ? (_currentTpSecondary != null ? _currentTpSecondary.transform : null) : null;

        Vector3 tracerStart;
        if (thirdPersonWeapon != null)
        {
            Transform tpFp = thirdPersonWeapon.Find("FirePoint");
            tracerStart = tpFp != null ? tpFp.position : thirdPersonWeapon.position;
        }
        else
        {
            tracerStart = transform.position + (Vector3.up * 1.5f);
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
        while (elapsedTime < _TRACER_ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(startPoint, endPoint, elapsedTime / _TRACER_ANIMATION_DURATION);
            if (lineRenderer != null) lineRenderer.SetPosition(1, currentPos);
            yield return null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(1, endPoint);
            yield return new WaitForSeconds(_TRACER_DISPLAY_HOLD_DURATION);
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
            PoolManager.Instance.ReturnTracer(lineRenderer);
        }
    }
}