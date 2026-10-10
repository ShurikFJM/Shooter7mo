using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponBase : NetworkBehaviour
{
    private const float _SPREAD_RECOVERY_SPEED = 14f;
    private const float _BURST_SPREAD_START = 0.015f;
    private const float _MAX_FIRING_PENALTY = 0.07f;
    private const float _MAX_TOTAL_SPREAD = 0.1f;
    private const float _RESET_COOLDOWN_PADDING = 0.12f;
    private const float _MUZZLE_FLASH_DURATION = 0.05f;
    private const float _IMPACT_LIFETIME = 8f;
    private const float _TRACER_DURATION = 0.04f;

    public WeaponData weaponData;
    public WeaponData WeaponData => weaponData;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTracerPrefab;

    [SerializeField] protected Transform _firePoint;
    [SerializeField] protected Transform _weaponModelTransform;
    [SerializeField] protected Animator _weaponAnimator;
    [SerializeField] protected LayerMask _hitLayers;
    [SerializeField] protected float _returnSpeed = 10f;

    protected NetworkPlayerController _playerController;
    protected AudioSource _audioSource;

    protected int _currentAmmo;
    protected int _reserveAmmo;
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
    }

    private void OnEnable()
    {
        if (_playerController == null) _playerController = GetComponentInParent<NetworkPlayerController>();

        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }

        if (_currentAmmo <= 0 && !_isReloading)
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
        if (_currentAmmo <= 0)
        {
            ResetAmmo();
        }

        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }
    }

    private void OnDisable()
    {
        CancelReload();
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
        }
    }

    private void Update()
    {
        if (_firingSpreadPenalty > 0f)
        {
            float currentFireRate = weaponData != null && weaponData.fireRate > 0f ? weaponData.fireRate : 0.15f;
            if (Time.time - _lastShotTime > (currentFireRate + _RESET_COOLDOWN_PADDING))
            {
                _continuousShots = 0;
                _firingSpreadPenalty = Mathf.MoveTowards(_firingSpreadPenalty, 0f, Time.deltaTime * _SPREAD_RECOVERY_SPEED);
            }
        }
    }

    public virtual float GetCurrentSpread()
    {
        bool isMoving = _playerController != null && _playerController.IsMoving;
        bool isGrounded = _playerController != null && _playerController.IsGrounded;

        float baseSpread = 0f;
        if (!isMoving && isGrounded && _continuousShots == 0)
        {
            return 0f;
        }

        float movementSpread = 0f;
        if (!isGrounded && weaponData != null)
        {
            baseSpread = weaponData.baseSpread * weaponData.airSpreadMultiplier;
            movementSpread = (weaponData.baseSpread + 0.02f) * weaponData.airSpreadMultiplier;
        }
        else if (isMoving && weaponData != null)
        {
            baseSpread = weaponData.baseSpread * weaponData.movementSpreadMultiplier;
            movementSpread = (weaponData.baseSpread + 0.01f) * weaponData.movementSpreadMultiplier;
        }
        else if (weaponData != null)
        {
            baseSpread = weaponData.baseSpread;
            movementSpread = weaponData.baseSpread;
        }

        if (!isMoving && isGrounded && _firingSpreadPenalty <= 0.0001f)
        {
            return 0f;
        }

        float burstSpread = _continuousShots > 0 ? (_BURST_SPREAD_START + _firingSpreadPenalty) : 0f;
        float totalSpread = movementSpread + burstSpread;

        return Mathf.Clamp(totalSpread, 0f, _MAX_TOTAL_SPREAD);
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
            rayDirection += alignTransform.right * UnityEngine.Random.Range(-currentSpread, currentSpread);
            rayDirection += alignTransform.up * UnityEngine.Random.Range(-currentSpread, currentSpread);
            rayDirection.Normalize();
        }

        _continuousShots++;
        if (weaponData != null)
        {
            _firingSpreadPenalty = Mathf.Min(_firingSpreadPenalty + weaponData.spreadPerShot, _MAX_FIRING_PENALTY);
        }

        float maxRange = weaponData != null ? weaponData.range : 100f;
        Vector3 targetPoint = rayOrigin + (rayDirection * maxRange);

        WeaponInventory inventory = _playerController != null ? _playerController.GetComponentInChildren<WeaponInventory>() : null;

        LayerMask maskToUse = _hitLayers != 0 ? _hitLayers : (LayerMask)~0;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRange, maskToUse, QueryTriggerInteraction.Ignore);
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

            targetPoint = hit.point;

            NetworkObject victimObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (victimObj != null)
            {
                if (IsOwner && inventory != null)
                {
                    inventory.RequestDealDamageServerRpc(victimObj, default);
                }
                break;
            }
            else
            {
                CreateImpactVisual(hit);
                break;
            }
        }

        if (IsOwner && inventory != null)
        {
            inventory.BroadcastShootServerRpc(targetPoint);
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

    protected virtual void CreateImpactVisual(RaycastHit hit)
    {
        if (weaponData != null && weaponData.impactPrefabs != null && weaponData.impactPrefabs.Length > 0 && PoolManager.Instance != null)
        {
            GameObject randomImpact = weaponData.impactPrefabs[Random.Range(0, weaponData.impactPrefabs.Length)];

            
            
            Quaternion impactRotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            
            Vector3 impactPosition = hit.point + (hit.normal * 0.01f);

            PoolManager.Instance.SpawnImpact(randomImpact, impactPosition, impactRotation, _IMPACT_LIFETIME);
        }
    }
    protected virtual void CreateTracerVisual(Vector3 targetPoint)
    {
        if (muzzleFlashPrefab != null && _firePoint != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.SpawnImpact(muzzleFlashPrefab, _firePoint.position, _firePoint.rotation, _MUZZLE_FLASH_DURATION);
        }

        if (_firePoint != null)
        {
            StartCoroutine(RenderTracerRoutine(_firePoint.position, targetPoint));
        }
    }

    private IEnumerator RenderTracerRoutine(Vector3 start, Vector3 end)
    {
        if (PoolManager.Instance == null) yield break;

        LineRenderer tracer = PoolManager.Instance.GetTracer(null);
        if (tracer == null) yield break;

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, start);

        float distance = Vector3.Distance(start, end);
        float duration = _TRACER_DURATION;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / duration);
            tracer.SetPosition(1, currentPos);
            yield return null;
        }

        tracer.SetPosition(1, end);
        yield return new WaitForSeconds(0.05f);
        PoolManager.Instance.ReturnTracer(tracer);
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