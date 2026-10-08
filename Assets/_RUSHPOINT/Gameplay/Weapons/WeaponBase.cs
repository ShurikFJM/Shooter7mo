using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponBase : MonoBehaviour
{
    private const float _SPREAD_RECOVERY_SPEED = 14f;
    private const float _MAX_SPREAD_PENALTY = 0.08f;
    private const float _MUZZLE_FLASH_DURATION = 0.05f;
    private const float _IMPACT_LIFETIME = 10f;
    private const float _TRACER_DURATION = 0.04f;
    private const float _TRACER_DESTROY_DELAY = 0.05f;
    private const float _DEFAULT_DAMAGE = 25f;
    private const float _DEFAULT_RELOAD_TIME = 2f;
    private const float _DEFAULT_BASE_SPREAD = 0.005f;

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
    private int _reloadTriggerHash;

    public WeaponData WeaponData => _data;
    public int CurrentAmmo => _currentAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public int MagazineCapacity => _data != null ? _data.maxAmmo : 30; [cite: 19]
    public int MaxReserveAmmo => _data != null ? _data.maxAmmo * _data.maxReserveMagazines : 90; [cite: 19]
    public int MaxAmmo => _data != null ? _data.maxAmmo : 30; [cite: 19]
    public bool IsReloading => _isReloading;
    public float ReloadDuration => _data != null ? _data.reloadTime : _DEFAULT_RELOAD_TIME; [cite: 19]
    public float ReloadProgressNormalized => ReloadDuration > 0f ? Mathf.Clamp01(1f - (_reloadRemainingTime / ReloadDuration)) : 0f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null)
        {
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
        }

        if (_playerController == null)
        {
            _playerController = GetComponentInParent<NetworkPlayerController>();
        }

        if (_weaponModelTransform == null)
        {
            _weaponModelTransform = transform;
        }

        _defaultLocalPosition = _weaponModelTransform.localPosition;
        _defaultLocalRotation = _weaponModelTransform.localRotation;
        _reloadTriggerHash = Animator.StringToHash(_reloadAnimationTrigger);

        InitializeAmmoFromData();
    }

    private void OnEnable()
    {
        if (_playerController == null)
        {
            _playerController = GetComponentInParent<NetworkPlayerController>();
        }

        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }

        if (_currentAmmo <= 0 && !_isReloading)
        {
            InitializeAmmoFromData();
        }
    }

    private void Start()
    {
        if (_currentAmmo <= 0)
        {
            InitializeAmmoFromData();
        }

        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }
    }

    private void InitializeAmmoFromData()
    {
        if (_data != null)
        {
            _currentAmmo = _data.maxAmmo; [cite: 19]
            _reserveAmmo = _data.maxAmmo * _data.maxReserveMagazines; [cite: 19]
        }
    }

    private void Update()
    {
        if (_playerController != null && !_playerController.IsOwner)
        {
            return;
        }

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            return;
        }

        BombInteractor bombInteractor = GetComponentInParent<BombInteractor>();
        if (bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing))
        {
            return;
        }

        _targetOffsetPosition = Vector3.Lerp(_targetOffsetPosition, Vector3.zero, Time.deltaTime * _returnSpeed);
        _targetOffsetRotation = Quaternion.Slerp(_targetOffsetRotation, Quaternion.identity, Time.deltaTime * _returnSpeed);

        Vector3 desiredPosition = _defaultLocalPosition + _targetOffsetPosition;
        Quaternion desiredRotation = _defaultLocalRotation * _targetOffsetRotation;

        _weaponModelTransform.localPosition = Vector3.Lerp(_weaponModelTransform.localPosition, desiredPosition, Time.deltaTime * _returnSpeed * 2f);
        _weaponModelTransform.localRotation = Quaternion.Slerp(_weaponModelTransform.localRotation, desiredRotation, Time.deltaTime * _returnSpeed * 2f);

        if (_firingSpreadPenalty > 0f)
        {
            _firingSpreadPenalty = Mathf.MoveTowards(_firingSpreadPenalty, 0f, Time.deltaTime * _SPREAD_RECOVERY_SPEED);
        }

        if (_isReloading)
        {
            _reloadRemainingTime -= Time.deltaTime;
        }
    }

    public virtual float GetCurrentSpread()
    {
        float totalSpread = _data != null ? _data.baseSpread : _DEFAULT_BASE_SPREAD; [cite: 19]
        totalSpread += _firingSpreadPenalty;

        if (_playerController != null)
        {
            if (!_playerController.IsGrounded && _data != null)
            {
                totalSpread *= _data.airSpreadMultiplier; [cite: 19]
            }
            else if (_playerController.IsMoving && _data != null)
            {
                totalSpread *= _data.movementSpreadMultiplier; [cite: 19]
            }
        }

        return Mathf.Clamp(totalSpread, 0f, 0.12f);
    }

    public virtual bool CanFire()
    {
        return !_isReloading && _currentAmmo > 0 && Time.time >= _nextTimeToFire;
    }

    public virtual void Fire()
    {
        if (!CanFire())
        {
            return;
        }

        Shoot();
    }

    protected virtual void Shoot()
    {
        _currentAmmo--;
        float fireRate = _data != null && _data.fireRate > 0f ? _data.fireRate : 0.15f; [cite: 19]
        _nextTimeToFire = Time.time + fireRate;

        if (_data != null)
        {
            _firingSpreadPenalty = Mathf.Min(_firingSpreadPenalty + _data.spreadPerShot, _MAX_SPREAD_PENALTY); [cite: 19]
        }

        PlayRandomShootSound();

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

        float maxRange = _data != null ? _data.range : 100f; [cite: 19]
        Vector3 targetPoint = rayOrigin + (rayDirection * maxRange);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myRootTransform = _playerController != null ? _playerController.transform : transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.IsChildOf(myRootTransform))
            {
                continue;
            }

            targetPoint = hit.point;

            Hitbox hitTarget = hit.collider.GetComponent<Hitbox>();
            if (hitTarget != null)
            {
                float baseDamage = _data != null ? _data.damage : _DEFAULT_DAMAGE; [cite: 19]
                ulong attackerId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
                hitTarget.ReceiveHit(baseDamage, attackerId);
            }
            else
            {
                CreateImpactVisual(hit);
            }

            break;
        }

        _targetOffsetPosition += _kickbackOffset;
        _targetOffsetRotation *= Quaternion.Euler(_kickbackRotation);

        TriggerMuzzleFlash();

        Vector3 tracerStart = _firePoint != null ? _firePoint.position : rayOrigin;
        StartCoroutine(RenderTracer(tracerStart, targetPoint));
    }

    private void PlayRandomShootSound()
    {
        if (_data != null && _data.shootSounds != null && _data.shootSounds.Length > 0 && _audioSource != null) [cite: 19]
        {
            int randomIndex = Random.Range(0, _data.shootSounds.Length); [cite: 19]
            AudioClip clip = _data.shootSounds[randomIndex]; [cite: 19]
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }

    public virtual void Reload()
    {
        if (_isReloading || _data == null)
        {
            return;
        }

        if (_currentAmmo >= _data.maxAmmo || _reserveAmmo <= 0) [cite: 19]
        {
            return;
        }

        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
        }

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
        float waitTime = _data != null ? _data.reloadTime : _DEFAULT_RELOAD_TIME; [cite: 19]
        _reloadRemainingTime = waitTime;

        if (_data != null && _data.reloadSound != null && _audioSource != null) [cite: 19]
        {
            _audioSource.PlayOneShot(_data.reloadSound); [cite: 19]
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
            _weaponAnimator.SetTrigger(_reloadTriggerHash);
        }

        yield return new WaitForSeconds(waitTime);

        int neededAmmo = _data.maxAmmo - _currentAmmo; [cite: 19]
        int ammoToTransfer = Mathf.Min(neededAmmo, _reserveAmmo);

        _currentAmmo += ammoToTransfer;
        _reserveAmmo -= ammoToTransfer;

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _reloadCoroutine = null;
        _firingSpreadPenalty = 0f;
    }

    private void TriggerMuzzleFlash()
    {
        if (_muzzleFlash == null)
        {
            return;
        }

        if (_muzzleFlashCoroutine != null)
        {
            StopCoroutine(_muzzleFlashCoroutine);
        }

        _muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        _muzzleFlash.gameObject.SetActive(true);
        _muzzleFlash.Clear();
        _muzzleFlash.Play();

        yield return new WaitForSeconds(_MUZZLE_FLASH_DURATION);

        _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _muzzleFlash.gameObject.SetActive(false);
    }

    private void CreateImpactVisual(RaycastHit hit)
    {
        if (_data != null && _data.impactPrefabs != null && _data.impactPrefabs.Length > 0) [cite: 19]
        {
            int randomIndex = Random.Range(0, _data.impactPrefabs.Length); [cite: 19]
            GameObject selectedPrefab = _data.impactPrefabs[randomIndex]; [cite: 19]

            if (selectedPrefab != null)
            {
                Quaternion impactRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0f, 180f, 0f);
                GameObject impact = Instantiate(selectedPrefab, hit.point + (hit.normal * 0.01f), impactRotation);
                Destroy(impact, _IMPACT_LIFETIME);
            }
        }
    }

    private IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObj = new GameObject("BulletTracer");
        LineRenderer line = tracerObj.AddComponent<LineRenderer>();

        line.startWidth = 0.02f;
        line.endWidth = 0.005f;
        line.material = _tracerMaterial != null ? _tracerMaterial : new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.yellow;
        line.endColor = new Color(1f, 0.4f, 0f, 0f);

        line.SetPosition(0, start);
        line.SetPosition(1, start);

        float elapsedTime = 0f;

        while (elapsedTime < _TRACER_DURATION)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / _TRACER_DURATION);
            line.SetPosition(1, currentPos);
            yield return null;
        }

        line.SetPosition(1, end);
        Destroy(tracerObj, _TRACER_DESTROY_DELAY);
    }

    public void ResetAmmo()
    {
        CancelReload();
        InitializeAmmoFromData();
    }
}