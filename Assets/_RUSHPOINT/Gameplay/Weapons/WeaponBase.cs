using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum SurfaceImpactType : byte
{
    None = 0,
    Wall = 1,
    Player = 2
}

[RequireComponent(typeof(AudioSource))]
public class WeaponBase : NetworkBehaviour
{
    private const float SPREAD_RECOVERY_SPEED = 18f;
    private const float BURST_SPREAD_START = 0.0012f;
    private const float MAX_SPREAD_PER_SHOT_STEP = 0.004f;
    private const float MAX_FIRING_PENALTY = 0.028f;
    private const float MAX_TOTAL_SPREAD = 0.048f;
    private const int CONTROLLED_BURST_SHOT_COUNT = 4;
    private const float MOVEMENT_BASE_SPREAD_OFFSET = 0.004f;
    private const float AIR_BASE_SPREAD_OFFSET = 0.012f;
    private const float RESET_COOLDOWN_PADDING = 0.1f;
    private const float MUZZLE_FLASH_DURATION = 0.035f;
    private const float IMPACT_LIFETIME = 8f;
    private const float PARTICLE_IMPACT_LIFETIME = 0.6f;
    private const float TRACER_DURATION = 0.04f;
    private const float IMPACT_AUDIO_MIN_DISTANCE = 3f;
    private const float IMPACT_AUDIO_MAX_DISTANCE = 35f;

    public WeaponData weaponData;
    public WeaponData WeaponData => weaponData;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTracerPrefab;

    [SerializeField] protected Transform _firePoint;
    [SerializeField] protected Transform _weaponModelTransform;
    [SerializeField] protected Animator _weaponAnimator;
    [SerializeField] protected LayerMask _hitLayers;
    [SerializeField] protected float _returnSpeed = 10f;
    [SerializeField] protected GameObject _wallImpactParticlePrefab;
    [SerializeField] protected GameObject _playerImpactParticlePrefab;
    [SerializeField] protected AudioClip _wallHitFallbackSound;
    [SerializeField] protected AudioClip _playerHitFallbackSound;

    protected NetworkPlayerController _playerController;
    protected AudioSource _audioSource;

    protected int _currentAmmo;
    protected int _reserveAmmo;
    protected bool _isAmmoInitialized;
    protected bool _isReloading;
    protected float _reloadRemainingTime;
    protected Coroutine _reloadCoroutine;
    protected float _nextTimeToFire;
    protected float _firingSpreadPenalty;
    protected float _lastShotTime;
    protected int _continuousShots;
    protected int _reloadTriggerHash = Animator.StringToHash("Reload");
    protected int _shootTriggerHash = Animator.StringToHash("Shoot");
    protected Vector3 _defaultLocalPosition;
    protected Quaternion _defaultLocalRotation;

    private GameObject _loadedMuzzleVfx;
    private GameObject _loadedWallVfx;
    private GameObject _loadedPlayerVfx;
    private AsyncOperationHandle<GameObject> _muzzleHandle;
    private AsyncOperationHandle<GameObject> _wallHandle;
    private AsyncOperationHandle<GameObject> _playerHandle;

    public int CurrentAmmo => _currentAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public bool IsReloading => _isReloading;
    public float ReloadProgressNormalized => weaponData != null && weaponData.reloadTime > 0 ? 1f - (_reloadRemainingTime / weaponData.reloadTime) : 0f;

    protected virtual void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_weaponModelTransform != null)
        {
            _defaultLocalPosition = _weaponModelTransform.localPosition;
            _defaultLocalRotation = _weaponModelTransform.localRotation;
        }

        EnsureFirePoint();
        PreloadAddressableVfx();
    }

    private void OnEnable()
    {
        if (_playerController == null) _playerController = GetComponentInParent<NetworkPlayerController>();

        EnsureFirePoint();

        if (!_isAmmoInitialized)
        {
            ResetAmmo();
        }

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _reloadCoroutine = null;

        if (_weaponModelTransform != null)
        {
            _weaponModelTransform.localPosition = _defaultLocalPosition;
            _weaponModelTransform.localRotation = _defaultLocalRotation;
        }
    }

    private void Start()
    {
        if (!_isAmmoInitialized)
        {
            ResetAmmo();
        }

        EnsureFirePoint();
    }

    private void OnDisable()
    {
        CancelReload();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_muzzleHandle.IsValid()) Addressables.Release(_muzzleHandle);
        if (_wallHandle.IsValid()) Addressables.Release(_wallHandle);
        if (_playerHandle.IsValid()) Addressables.Release(_playerHandle);
    }

    public void EnsureFirePoint()
    {
        if (_firePoint != null && _firePoint.IsChildOf(transform))
        {
            return;
        }

        Transform foundFp = transform.Find("FirePoint");
        if (foundFp == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.Equals("FirePoint", System.StringComparison.OrdinalIgnoreCase) ||
                    children[i].name.Equals("MuzzlePoint", System.StringComparison.OrdinalIgnoreCase))
                {
                    foundFp = children[i];
                    break;
                }
            }
        }

        if (foundFp != null)
        {
            _firePoint = foundFp;
            return;
        }

        if (_playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }
    }

    private void PreloadAddressableVfx()
    {
        if (weaponData == null) return;

        if (muzzleFlashPrefab == null && weaponData.muzzleFlashVfxPrefab == null && !string.IsNullOrEmpty(weaponData.muzzleFlashAddressKey))
        {
            _muzzleHandle = Addressables.LoadAssetAsync<GameObject>(weaponData.muzzleFlashAddressKey);
            _muzzleHandle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded) _loadedMuzzleVfx = op.Result;
            };
        }

        if (_wallImpactParticlePrefab == null && weaponData.wallImpactVfxPrefab == null && !string.IsNullOrEmpty(weaponData.wallImpactAddressKey))
        {
            _wallHandle = Addressables.LoadAssetAsync<GameObject>(weaponData.wallImpactAddressKey);
            _wallHandle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded) _loadedWallVfx = op.Result;
            };
        }

        if (_playerImpactParticlePrefab == null && weaponData.playerImpactVfxPrefab == null && !string.IsNullOrEmpty(weaponData.playerImpactAddressKey))
        {
            _playerHandle = Addressables.LoadAssetAsync<GameObject>(weaponData.playerImpactAddressKey);
            _playerHandle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded) _loadedPlayerVfx = op.Result;
            };
        }
    }

    public void UpdateDefaultTransform(Vector3 pos, Quaternion rot)
    {
        _defaultLocalPosition = pos;
        _defaultLocalRotation = rot;
    }

    public void ResetAmmo()
    {
        if (weaponData != null)
        {
            _currentAmmo = weaponData.maxAmmo;
            _reserveAmmo = weaponData.maxReserveMagazines * weaponData.maxAmmo;
            _isAmmoInitialized = true;
        }
    }

    private void Update()
    {
        if (_firingSpreadPenalty > 0f || _continuousShots > 0)
        {
            float currentFireRate = weaponData != null && weaponData.fireRate > 0f ? weaponData.fireRate : 0.15f;
            if (Time.time - _lastShotTime > (currentFireRate + RESET_COOLDOWN_PADDING))
            {
                _firingSpreadPenalty = Mathf.MoveTowards(_firingSpreadPenalty, 0f, Time.deltaTime * SPREAD_RECOVERY_SPEED);
                if (_firingSpreadPenalty <= 0.0001f)
                {
                    _firingSpreadPenalty = 0f;
                    _continuousShots = 0;
                }
            }
        }
    }

    public virtual float GetCurrentSpread()
    {
        bool isMoving = _playerController != null && _playerController.IsMoving;
        bool isGrounded = _playerController != null && _playerController.IsGrounded;

        if (!isMoving && isGrounded && _continuousShots == 0)
        {
            return 0f;
        }

        float movementSpread = 0f;
        if (!isGrounded && weaponData != null)
        {
            float clampedAirMult = Mathf.Min(weaponData.airSpreadMultiplier, 4.5f);
            movementSpread = (weaponData.baseSpread + AIR_BASE_SPREAD_OFFSET) * clampedAirMult;
        }
        else if (isMoving && weaponData != null)
        {
            float clampedMoveMult = Mathf.Min(weaponData.movementSpreadMultiplier, 2.0f);
            movementSpread = (weaponData.baseSpread + MOVEMENT_BASE_SPREAD_OFFSET) * clampedMoveMult;
        }
        else if (weaponData != null)
        {
            movementSpread = weaponData.baseSpread;
        }

        if (!isMoving && isGrounded && _firingSpreadPenalty <= 0.0001f)
        {
            return 0f;
        }

        float burstProgression = Mathf.Clamp01((float)_continuousShots / CONTROLLED_BURST_SHOT_COUNT);
        float smoothRampFactor = burstProgression * burstProgression;
        float burstSpread = _continuousShots > 0 ? (BURST_SPREAD_START + (_firingSpreadPenalty * smoothRampFactor)) : 0f;
        float totalSpread = movementSpread + burstSpread;

        return Mathf.Clamp(totalSpread, 0f, MAX_TOTAL_SPREAD);
    }

    public bool CanFire()
    {
        if (_isReloading || weaponData == null) return false;
        if (Time.time < _nextTimeToFire) return false;
        if (_currentAmmo <= 0) return false;
        return true;
    }

    public virtual void Fire()
    {
        if (!CanFire()) return;
        Shoot();
    }

    protected virtual void Shoot()
    {
        _currentAmmo--;
        float fireRate = weaponData != null && weaponData.fireRate > 0f ? weaponData.fireRate : 0.15f;
        _nextTimeToFire = Time.time + fireRate;
        _lastShotTime = Time.time;

        PlayRandomShootSound();

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_shootTriggerHash);
        }

        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;
        Transform alignTransform = transform;

        if (_playerController != null && _playerController.PlayerCamera != null)
        {
            rayOrigin = _playerController.PlayerCamera.transform.position;
            rayDirection = _playerController.PlayerCamera.transform.forward;
            alignTransform = _playerController.PlayerCamera.transform;
        }

        float currentSpread = GetCurrentSpread();
        if (currentSpread > 0f)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * currentSpread;
            rayDirection += (alignTransform.right * randomCircle.x) + (alignTransform.up * randomCircle.y);
            rayDirection.Normalize();
        }

        _continuousShots++;
        if (weaponData != null)
        {
            float sanitizedStep = Mathf.Min(weaponData.spreadPerShot, MAX_SPREAD_PER_SHOT_STEP);
            _firingSpreadPenalty = Mathf.Min(_firingSpreadPenalty + sanitizedStep, MAX_FIRING_PENALTY);
        }

        float maxRange = weaponData != null ? weaponData.range : 100f;
        Vector3 targetPoint = rayOrigin + (rayDirection * maxRange);
        Vector3 hitNormal = -rayDirection;
        SurfaceImpactType impactType = SurfaceImpactType.None;

        WeaponInventory inventory = _playerController != null ? _playerController.GetComponentInChildren<WeaponInventory>() : null;

        LayerMask maskToUse = _hitLayers != 0 ? _hitLayers : (LayerMask)~0;
        int hitboxLayer = LayerMask.NameToLayer("Hitbox");
        if (hitboxLayer != -1)
        {
            maskToUse |= (1 << hitboxLayer);
        }

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRange, maskToUse, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myRootTransform = _playerController != null ? _playerController.transform : transform.root;
        Transform myShooterTransform = _playerController != null ? _playerController.transform : transform;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.transform.IsChildOf(myShooterTransform) || hit.transform == myShooterTransform || hit.transform.IsChildOf(myRootTransform))
            {
                continue;
            }

            if (hit.collider.isTrigger && hit.collider.GetComponent<Hitbox>() == null)
            {
                continue;
            }

            targetPoint = hit.point;
            hitNormal = hit.normal;

            NetworkHealth victimHealth = hit.collider.GetComponentInParent<NetworkHealth>();
            NetworkObject victimObj = hit.collider.GetComponentInParent<NetworkObject>();

            if (victimHealth != null && victimObj != null)
            {
                impactType = SurfaceImpactType.Player;
                CreatePlayerImpactVisual(hit.point, hit.normal);
                PlayImpactSound(hit.point, true);

                RaycastHit bestHit = hit;
                Hitbox bestHitbox = hit.collider.GetComponent<Hitbox>();

                for (int j = 0; j < hits.Length; j++)
                {
                    if (hits[j].transform.IsChildOf(victimObj.transform))
                    {
                        Hitbox candidate = hits[j].collider.GetComponent<Hitbox>();
                        if (candidate != null)
                        {
                            bestHit = hits[j];
                            bestHitbox = candidate;
                            if (candidate.Type == HitboxType.Head)
                            {
                                break;
                            }
                        }
                        else if (hits[j].collider.name.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            bestHit = hits[j];
                            break;
                        }
                    }
                }

                targetPoint = bestHit.point;
                hitNormal = bestHit.normal;

                HitboxType hitboxType = HitboxType.Chest;
                if (bestHitbox != null)
                {
                    hitboxType = bestHitbox.Type;
                }
                else if (bestHit.collider.name.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hitboxType = HitboxType.Head;
                }

                if (inventory != null && inventory.IsOwner)
                {
                    inventory.RequestDealDamageServerRpc(victimObj, hitboxType);
                }

                Rigidbody hitRb = bestHit.collider.attachedRigidbody != null ? bestHit.collider.attachedRigidbody : hit.collider.attachedRigidbody;
                if (hitRb != null && !hitRb.isKinematic)
                {
                    hitRb.AddForceAtPosition(rayDirection * 150f, bestHit.point, ForceMode.Impulse);
                }

                RagdollController ragdoll = victimObj.GetComponentInChildren<RagdollController>();
                if (ragdoll != null && (!victimHealth.IsAlive.Value || victimHealth.CurrentHealth.Value <= 0f))
                {
                    ragdoll.TriggerRagdoll(rayDirection, 200f);
                }

                break;
            }
            else
            {
                impactType = SurfaceImpactType.Wall;
                CreateImpactVisual(hit);
                PlayImpactSound(hit.point, false);
                break;
            }
        }

        if (inventory != null && inventory.IsOwner)
        {
            inventory.BroadcastShootServerRpc(targetPoint, hitNormal, (byte)impactType);
        }

        CreateTracerVisual(targetPoint);
    }

    protected virtual void PlayRandomShootSound()
    {
        if (weaponData != null && weaponData.shootSounds != null && weaponData.shootSounds.Length > 0 && _audioSource != null)
        {
            AudioClip clip = weaponData.shootSounds[Random.Range(0, weaponData.shootSounds.Length)];
            _audioSource.PlayOneShot(clip);
        }
    }

    public void PlayImpactSound(Vector3 impactPosition, bool isPlayerHit)
    {
        AudioClip selectedClip = null;

        if (isPlayerHit)
        {
            if (weaponData != null && weaponData.playerImpactSounds != null && weaponData.playerImpactSounds.Length > 0)
            {
                selectedClip = weaponData.playerImpactSounds[Random.Range(0, weaponData.playerImpactSounds.Length)];
            }
            else
            {
                selectedClip = _playerHitFallbackSound;
            }

            if (selectedClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(selectedClip);
            }
        }
        else
        {
            if (weaponData != null && weaponData.wallImpactSounds != null && weaponData.wallImpactSounds.Length > 0)
            {
                selectedClip = weaponData.wallImpactSounds[Random.Range(0, weaponData.wallImpactSounds.Length)];
            }
            else
            {
                selectedClip = _wallHitFallbackSound;
            }

            if (selectedClip != null)
            {
                SpawnSpatialImpactAudio(selectedClip, impactPosition);
            }
        }
    }

    private void SpawnSpatialImpactAudio(AudioClip clip, Vector3 position)
    {
        GameObject audioHost = new GameObject("SurfaceImpactAudio");
        audioHost.transform.position = position;
        AudioSource spatialSource = audioHost.AddComponent<AudioSource>();
        spatialSource.spatialBlend = 1f;
        spatialSource.rolloffMode = AudioRolloffMode.Linear;
        spatialSource.minDistance = IMPACT_AUDIO_MIN_DISTANCE;
        spatialSource.maxDistance = IMPACT_AUDIO_MAX_DISTANCE;
        spatialSource.clip = clip;
        spatialSource.Play();
        Destroy(audioHost, clip.length + 0.1f);
    }

    protected virtual void CreateImpactVisual(RaycastHit hit)
    {
        CreateWallImpactVisual(hit.point, hit.normal);
    }

    public void CreateWallImpactVisual(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (weaponData != null && weaponData.impactPrefabs != null && weaponData.impactPrefabs.Length > 0 && PoolManager.Instance != null)
        {
            GameObject randomImpact = weaponData.impactPrefabs[Random.Range(0, weaponData.impactPrefabs.Length)];
            Quaternion decalRotation = Quaternion.LookRotation(-hitNormal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            Vector3 decalPosition = hitPoint + (hitNormal * 0.01f);

            PoolManager.Instance.SpawnImpact(randomImpact, decalPosition, decalRotation, IMPACT_LIFETIME);
        }

        GameObject wallVfx = GetResolvedWallImpactPrefab();
        if (wallVfx != null)
        {
            Quaternion particleRotation = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
            Vector3 particlePosition = hitPoint + (hitNormal * 0.02f);
            SpawnVfxInstance(wallVfx, particlePosition, particleRotation, PARTICLE_IMPACT_LIFETIME);
        }
    }

    public void CreatePlayerImpactVisual(Vector3 hitPoint, Vector3 hitNormal)
    {
        GameObject playerVfx = GetResolvedPlayerImpactPrefab();
        if (playerVfx != null)
        {
            Quaternion particleRotation = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
            SpawnVfxInstance(playerVfx, hitPoint, particleRotation, PARTICLE_IMPACT_LIFETIME);
        }
    }

    protected virtual void CreateTracerVisual(Vector3 targetPoint)
    {
        EnsureFirePoint();

        GameObject muzzleVfx = GetResolvedMuzzleFlashPrefab();
        if (muzzleVfx != null && _firePoint != null)
        {
            SpawnVfxInstance(muzzleVfx, _firePoint.position, _firePoint.rotation, MUZZLE_FLASH_DURATION);
        }

        if (_firePoint != null)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.StartCoroutine(RenderTracerRoutine(_firePoint.position, targetPoint));
            }
            else
            {
                StartCoroutine(RenderTracerRoutine(_firePoint.position, targetPoint));
            }
        }
    }

    private void SpawnVfxInstance(GameObject prefab, Vector3 position, Quaternion rotation, float duration)
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.SpawnImpact(prefab, position, rotation, duration);
        }
        else
        {
            GameObject instance = Instantiate(prefab, position, rotation);
            Destroy(instance, duration);
        }
    }

    private GameObject GetResolvedMuzzleFlashPrefab()
    {
        if (muzzleFlashPrefab != null) return muzzleFlashPrefab;
        if (weaponData != null && weaponData.muzzleFlashVfxPrefab != null) return weaponData.muzzleFlashVfxPrefab;
        return _loadedMuzzleVfx;
    }

    private GameObject GetResolvedWallImpactPrefab()
    {
        if (_wallImpactParticlePrefab != null) return _wallImpactParticlePrefab;
        if (weaponData != null && weaponData.wallImpactVfxPrefab != null) return weaponData.wallImpactVfxPrefab;
        return _loadedWallVfx;
    }

    private GameObject GetResolvedPlayerImpactPrefab()
    {
        if (_playerImpactParticlePrefab != null) return _playerImpactParticlePrefab;
        if (weaponData != null && weaponData.playerImpactVfxPrefab != null) return weaponData.playerImpactVfxPrefab;
        return _loadedPlayerVfx;
    }

    private IEnumerator RenderTracerRoutine(Vector3 start, Vector3 end)
    {
        if (bulletTracerPrefab != null)
        {
            Vector3 direction = (end - start).normalized;
            Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            GameObject tracerObj = Instantiate(bulletTracerPrefab, start, rotation);

            Light tracerLight = tracerObj.GetComponentInChildren<Light>();
            if (tracerLight != null)
            {
                tracerLight.enabled = true;
            }

            float duration = TRACER_DURATION;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                if (tracerObj != null)
                {
                    tracerObj.transform.position = Vector3.Lerp(start, end, t);
                }

                yield return null;
            }

            if (tracerObj != null)
            {
                tracerObj.transform.position = end;
                Destroy(tracerObj, 0.04f);
            }
        }
        else if (PoolManager.Instance != null)
        {
            LineRenderer lineRenderer = PoolManager.Instance.GetTracer(null);
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, start);

                float duration = TRACER_DURATION;
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / duration);
                    if (lineRenderer != null) lineRenderer.SetPosition(1, currentPos);
                    yield return null;
                }

                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(1, end);
                    yield return new WaitForSeconds(0.02f);
                    lineRenderer.positionCount = 0;
                    lineRenderer.enabled = false;
                    PoolManager.Instance.ReturnTracer(lineRenderer);
                }
            }
        }
    }

    public void Reload()
    {
        if (_isReloading || weaponData == null || _currentAmmo == weaponData.maxAmmo || _reserveAmmo <= 0) return;

        _isReloading = true;

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_reloadTriggerHash);
        }

        if (weaponData.reloadSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(weaponData.reloadSound);
        }

        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        _reloadRemainingTime = weaponData != null ? weaponData.reloadTime : 1.5f;

        while (_reloadRemainingTime > 0)
        {
            _reloadRemainingTime -= Time.deltaTime;
            yield return null;
        }

        if (weaponData != null)
        {
            int ammoNeeded = weaponData.maxAmmo - _currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, _reserveAmmo);

            _currentAmmo += ammoToReload;
            _reserveAmmo -= ammoToReload;
        }

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _reloadCoroutine = null;
        _continuousShots = 0;
        _firingSpreadPenalty = 0f;
    }

    public void CancelReload()
    {
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
        }

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _continuousShots = 0;
        _firingSpreadPenalty = 0f;

        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
        }
    }
} 