using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponBase : MonoBehaviour
{
    private const float SPREAD_RECOVERY_SPEED = 14f;
    private const float BURST_SPREAD_START = 0.015f;
    private const float MAX_FIRING_PENALTY = 0.07f;
    private const float MAX_TOTAL_SPREAD = 0.1f;
    private const float RESET_COOLDOWN_PADDING = 0.12f;
    private const float MUZZLE_FLASH_DURATION = 0.05f;
    private const float IMPACT_LIFETIME = 10f;
    private const float TRACER_DURATION = 0.04f;
    private const float TRACER_DESTROY_DELAY = 0.05f;
    private const float DEFAULT_DAMAGE = 25f;
    private const float DEFAULT_RELOAD_TIME = 2f;

    [SerializeField] protected WeaponData _data;
    [SerializeField] private Material _tracerMaterial;
    [SerializeField] private Transform _weaponModelTransform;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private string _reloadAnimationTrigger = "Reload";
    [SerializeField] private Vector3 _kickbackOffset = new Vector3(0f, 0f, -0.03f);
    [SerializeField] private Vector3 _kickbackRotation = new Vector3(-1.5f, 0f, 0f);
    [SerializeField] private float _returnSpeed = 12f;
    [SerializeField] private int _currentAmmo;
    [SerializeField] private int _reserveAmmo;
    [SerializeField] private bool _isReloading;
    [SerializeField] private float _reloadRemainingTime;

    private AudioSource _audioSource;
    private NetworkPlayerController _playerController;
    private Vector3 _defaultLocalPosition;
    private Quaternion _defaultLocalRotation;
    private Vector3 _targetOffsetPosition;
    private Quaternion _targetOffsetRotation = Quaternion.identity;
    private Coroutine _muzzleFlashCoroutine;
    private Coroutine _reloadCoroutine;
    private float _nextTimeToFire;
    private float _firingSpreadPenalty;
    private float _lastShotTime;
    private int _continuousShots;
    private int _reloadTriggerHash;

    public WeaponData WeaponData => _data;
    public int CurrentAmmo => _currentAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public int MagazineCapacity => _data != null ? _data.maxAmmo : 30;
    public int MaxReserveAmmo => _data != null ? _data.maxAmmo * _data.maxReserveMagazines : 90;
    public int MaxAmmo => _data != null ? _data.maxAmmo : 30;
    public bool IsReloading => _isReloading;
    public float ReloadDuration => _data != null ? _data.reloadTime : DEFAULT_RELOAD_TIME;
    public float ReloadProgressNormalized => ReloadDuration > 0f ? Mathf.Clamp01(1f - (_reloadRemainingTime / ReloadDuration)) : 0f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null)
        {
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
        }

        if (_playerController == null) _playerController = GetComponentInParent<NetworkPlayerController>();
        if (_weaponModelTransform == null) _weaponModelTransform = transform;

        _defaultLocalPosition = _weaponModelTransform.localPosition;
        _defaultLocalRotation = _weaponModelTransform.localRotation;
        _reloadTriggerHash = Animator.StringToHash(_reloadAnimationTrigger);

        InitializeAmmoFromData();
    }

    private void OnEnable()
    {
        if (_playerController == null) _playerController = GetComponentInParent<NetworkPlayerController>();
        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _reloadCoroutine = null;
    }

    private void Start()
    {
        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }
    }

    private void OnDisable()
    {
        CancelReload();
    }

    private void InitializeAmmoFromData()
    {
        if (_data != null)
        {
            _currentAmmo = _data.maxAmmo;
            _reserveAmmo = _data.maxAmmo * _data.maxReserveMagazines;
        }
    }

    private void Update()
    {
        if (_playerController != null && !_playerController.IsOwner) return;
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;

        BombInteractor bombInteractor = GetComponentInParent<BombInteractor>();
        if (bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing)) return;

        _targetOffsetPosition = Vector3.Lerp(_targetOffsetPosition, Vector3.zero, Time.deltaTime * _returnSpeed);
        _targetOffsetRotation = Quaternion.Slerp(_targetOffsetRotation, Quaternion.identity, Time.deltaTime * _returnSpeed);

        Vector3 desiredPosition = _defaultLocalPosition + _targetOffsetPosition;
        Quaternion desiredRotation = _defaultLocalRotation * _targetOffsetRotation;

        _weaponModelTransform.localPosition = Vector3.Lerp(_weaponModelTransform.localPosition, desiredPosition, Time.deltaTime * _returnSpeed * 2f);
        _weaponModelTransform.localRotation = Quaternion.Slerp(_weaponModelTransform.localRotation, desiredRotation, Time.deltaTime * _returnSpeed * 2f);

        float currentFireRate = _data != null && _data.fireRate > 0f ? _data.fireRate : 0.15f;
        if (Time.time - _lastShotTime > (currentFireRate + RESET_COOLDOWN_PADDING))
        {
            _continuousShots = 0;
            _firingSpreadPenalty = Mathf.MoveTowards(_firingSpreadPenalty, 0f, Time.deltaTime * SPREAD_RECOVERY_SPEED);
        }

        if (_isReloading) _reloadRemainingTime -= Time.deltaTime;
    }

    public virtual float GetCurrentSpread()
    {
        bool isMoving = _playerController != null && _playerController.IsMoving;
        bool isGrounded = _playerController != null && _playerController.IsGrounded;

        if (!isMoving && isGrounded && _continuousShots == 0) return 0f;

        float movementSpread = 0f;
        if (!isGrounded && _data != null) movementSpread = (_data.baseSpread + 0.02f) * _data.airSpreadMultiplier;
        else if (isMoving && _data != null) movementSpread = (_data.baseSpread + 0.01f) * _data.movementSpreadMultiplier;
        else if (_data != null) movementSpread = _data.baseSpread;

        float burstSpread = _continuousShots > 0 ? (BURST_SPREAD_START + _firingSpreadPenalty) : 0f;
        float totalSpread = movementSpread + burstSpread;

        return Mathf.Clamp(totalSpread, 0f, MAX_TOTAL_SPREAD);
    }

    public virtual bool CanFire()
    {
        return !_isReloading && _currentAmmo > 0 && Time.time >= _nextTimeToFire;
    }

    public virtual void Fire()
    {
        if (!CanFire()) return;
        Shoot();
    }

    protected virtual void Shoot()
    {
        _currentAmmo--;
        float fireRate = _data != null && _data.fireRate > 0f ? _data.fireRate : 0.15f;
        _nextTimeToFire = Time.time + fireRate;
        _lastShotTime = Time.time;

        PlayRandomShootSound();

        WeaponInventory inventory = GetComponentInParent<WeaponInventory>();
        Camera playerCam = _playerController != null ? _playerController.PlayerCamera : null;
        Transform camTransform = playerCam != null ? playerCam.transform : transform;

        Vector3 rayOrigin = camTransform.position;
        Vector3 rayDirection = camTransform.forward;

        float currentSpread = GetCurrentSpread();
        if (currentSpread > 0.0001f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * currentSpread;
            rayDirection += (camTransform.right * randomCircle.x) + (camTransform.up * randomCircle.y);
            rayDirection.Normalize();
        }

        _continuousShots++;
        if (_data != null) _firingSpreadPenalty = Mathf.Min(_firingSpreadPenalty + _data.spreadPerShot, MAX_FIRING_PENALTY);

        float maxRange = _data != null ? _data.range : 100f;
        Vector3 targetPoint = rayOrigin + (rayDirection * maxRange);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myShooterTransform = _playerController != null ? _playerController.transform : transform;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.IsChildOf(myShooterTransform) || hit.transform == myShooterTransform) continue;

            targetPoint = hit.point;

            Hitbox hitTarget = hit.collider.GetComponent<Hitbox>();
            if (hitTarget == null) hitTarget = hit.collider.GetComponentInParent<Hitbox>();

            if (hitTarget != null && hitTarget.TargetHealth != null)
            {
                NetworkObject victimNetObj = hitTarget.TargetHealth.GetComponent<NetworkObject>();
                if (victimNetObj != null && inventory != null) inventory.RequestDealDamageServerRpc(victimNetObj, hitTarget.Type);
                CreateImpactVisual(hit);
                break;
            }

            NetworkHealth targetHealth = hit.collider.GetComponentInParent<NetworkHealth>();
            if (targetHealth == null) targetHealth = hit.collider.GetComponentInChildren<NetworkHealth>();

            if (targetHealth != null && targetHealth.IsAlive.Value)
            {
                NetworkObject victimNetObj = targetHealth.GetComponent<NetworkObject>();
                if (victimNetObj != null && inventory != null) inventory.RequestDealDamageServerRpc(victimNetObj, HitboxType.Chest);
                CreateImpactVisual(hit);
                break;
            }

            CreateImpactVisual(hit);
            break;
        }

        _targetOffsetPosition += _kickbackOffset;
        _targetOffsetRotation *= Quaternion.Euler(_kickbackRotation);

        TriggerMuzzleFlash();

        Vector3 tracerStart = _firePoint != null ? _firePoint.position : rayOrigin;
        StartCoroutine(RenderTracer(tracerStart, targetPoint));

        if (inventory != null) inventory.BroadcastShootServerRpc(targetPoint);
    }

    private void PlayRandomShootSound()
    {
        if (_data != null && _data.shootSounds != null && _data.shootSounds.Length > 0 && _audioSource != null)
        {
            int randomIndex = Random.Range(0, _data.shootSounds.Length);
            AudioClip clip = _data.shootSounds[randomIndex];
            if (clip != null) _audioSource.PlayOneShot(clip);
        }
    }

    public virtual void Reload()
    {
        if (_isReloading || _data == null) return;
        if (_currentAmmo >= _data.maxAmmo || _reserveAmmo <= 0) return;

        if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    public void CancelReload()
    {
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _continuousShots = 0;
        _firingSpreadPenalty = 0f;

        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
            _weaponAnimator.Rebind();
            _weaponAnimator.Update(0f);
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        float waitTime = _data != null ? _data.reloadTime : DEFAULT_RELOAD_TIME;
        _reloadRemainingTime = waitTime;

        if (_data != null && _data.reloadSound != null && _audioSource != null) _audioSource.PlayOneShot(_data.reloadSound);
        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
            _weaponAnimator.SetTrigger(_reloadTriggerHash);
        }

        yield return new WaitForSeconds(waitTime);

        int neededAmmo = _data.maxAmmo - _currentAmmo;
        int ammoToTransfer = Mathf.Min(neededAmmo, _reserveAmmo);

        _currentAmmo += ammoToTransfer;
        _reserveAmmo -= ammoToTransfer;

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _reloadCoroutine = null;
        _continuousShots = 0;
        _firingSpreadPenalty = 0f;
    }

    private void TriggerMuzzleFlash()
    {
        if (_muzzleFlash == null) return;
        if (_muzzleFlashCoroutine != null) StopCoroutine(_muzzleFlashCoroutine);
        _muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        _muzzleFlash.gameObject.SetActive(true);
        _muzzleFlash.Clear();
        _muzzleFlash.Play();
        yield return new WaitForSeconds(MUZZLE_FLASH_DURATION);
        _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _muzzleFlash.gameObject.SetActive(false);
    }

    private void CreateImpactVisual(RaycastHit hit)
    {
        if (_data != null && _data.impactPrefabs != null && _data.impactPrefabs.Length > 0 && PoolManager.Instance != null)
        {
            int randomIndex = Random.Range(0, _data.impactPrefabs.Length);
            GameObject selectedPrefab = _data.impactPrefabs[randomIndex];

            if (selectedPrefab != null)
            {
                Quaternion impactRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0f, 180f, 0f);
                PoolManager.Instance.SpawnImpact(selectedPrefab, hit.point + (hit.normal * 0.01f), impactRotation, IMPACT_LIFETIME);
            }
        }
    }

    private IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        if (PoolManager.Instance == null) yield break;

        LineRenderer line = PoolManager.Instance.GetTracer(_tracerMaterial);
        line.SetPosition(0, start);
        line.SetPosition(1, start);

        float elapsedTime = 0f;
        while (elapsedTime < TRACER_DURATION)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / TRACER_DURATION);
            line.SetPosition(1, currentPos);
            yield return null;
        }

        line.SetPosition(1, end);
        yield return new WaitForSeconds(TRACER_DESTROY_DELAY);
        PoolManager.Instance.ReturnTracer(line);
    }

    public void ResetAmmo()
    {
        CancelReload();
        InitializeAmmoFromData();
    }

    public void UpdateDefaultTransform(Vector3 newLocalPos, Quaternion newLocalRot)
    {
        _defaultLocalPosition = newLocalPos;
        _defaultLocalRotation = newLocalRot;
    }
}