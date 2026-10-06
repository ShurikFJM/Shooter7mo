using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class WeaponBase : NetworkBehaviour
{
    private const float _TRIGGER_DEADZONE = 0.2f;
    private const float _SPREAD_RECOVERY_SPEED = 12f;
    private const float _DEFAULT_RECOIL_RESET_TIME = 0.3f;
    private const float _DEFAULT_FIRE_RATE = 0.1f;
    private const float _SPREAD_BURST_MULTIPLIER = 1.5f;
    private const float _AIR_SPREAD_FACTOR = 0.5f;
    private const float _MOVE_SPREAD_FACTOR = 0.35f;
    private const float _DEFAULT_RANGE = 100f;
    private const float _DEFAULT_DAMAGE = 25f;
    private const float _DEFAULT_RELOAD_TIME = 1.5f;
    private const float _MUZZLE_FLASH_DURATION = 0.05f;
    private const float _IMPACT_LIFETIME = 4f;
    private const float _TRACER_DURATION = 0.03f;
    private const float _TRACER_DESTROY_DELAY = 0.02f;
    private const float _TWO_PI = Mathf.PI * 2f;

    [SerializeField] private WeaponData _data;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private NetworkPlayerController _playerController;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private Material _tracerMaterial;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private Transform _weaponModelTransform;
    [SerializeField] private Vector3 _kickbackOffset = new Vector3(0f, 0.02f, -0.08f);
    [SerializeField] private Vector3 _kickbackRotation = new Vector3(-3f, 1f, 0f);
    [SerializeField] private float _returnSpeed = 15f;

    private int _currentAmmo;
    private bool _isReloading;
    private float _nextTimeToFire;
    private int _currentShotIndex;
    private float _lastShotTime;
    private float _firingSpreadPenalty;
    private bool _wasRtPressedLastFrame;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Coroutine _muzzleFlashCoroutine;
    private Coroutine _reloadCoroutine;
    private AudioSource _audioSource;

    private readonly int _shootTriggerHash = Animator.StringToHash("Shoot");
    private readonly int _reloadTriggerHash = Animator.StringToHash("Reload");

    public WeaponData data => _data;
    public WeaponData Data => _data;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _data != null ? _data.maxAmmo : 0;
    public bool IsReloading => _isReloading;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        if (_data != null)
        {
            _currentAmmo = _data.maxAmmo;
        }

        if (_playerController == null)
        {
            _playerController = GetComponentInParent<NetworkPlayerController>();
        }

        if (_firePoint == null && _playerController != null && _playerController.PlayerCamera != null)
        {
            _firePoint = _playerController.PlayerCamera.transform;
        }

        if (_weaponModelTransform == null)
        {
            _weaponModelTransform = transform;
        }

        if (_muzzleFlash != null)
        {
            _muzzleFlash.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CancelReload();

        _wasRtPressedLastFrame = false;
        _nextTimeToFire = 0f;

        _targetPosition = Vector3.zero;
        _targetRotation = Quaternion.identity;

        if (_weaponModelTransform != null)
        {
            _weaponModelTransform.localPosition = Vector3.zero;
            _weaponModelTransform.localRotation = Quaternion.identity;
        }

        if (_muzzleFlashCoroutine != null)
        {
            StopCoroutine(_muzzleFlashCoroutine);
            _muzzleFlashCoroutine = null;
        }

        if (_muzzleFlash != null)
        {
            _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _muzzleFlash.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen) return;

        if (RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value != RoundPhase.InProgress) return;

        BombInteractor bombInteractor = GetComponentInParent<BombInteractor>();
        if (bombInteractor != null && (bombInteractor.IsPlanting || bombInteractor.IsDefusing)) return;

        _targetPosition = Vector3.Lerp(_targetPosition, Vector3.zero, Time.deltaTime * _returnSpeed);
        _targetRotation = Quaternion.Slerp(_targetRotation, Quaternion.identity, Time.deltaTime * _returnSpeed);

        _weaponModelTransform.localPosition = Vector3.Lerp(_weaponModelTransform.localPosition, _targetPosition, Time.deltaTime * _returnSpeed * 2f);
        _weaponModelTransform.localRotation = Quaternion.Slerp(_weaponModelTransform.localRotation, _targetRotation, Time.deltaTime * _returnSpeed * 2f);

        if (_firingSpreadPenalty > 0f)
        {
            _firingSpreadPenalty = Mathf.Lerp(_firingSpreadPenalty, 0f, Time.deltaTime * _SPREAD_RECOVERY_SPEED);
        }

        float resetTime = _data != null ? _data.recoilResetTime : _DEFAULT_RECOIL_RESET_TIME;
        if (Time.time - _lastShotTime > resetTime)
        {
            _currentShotIndex = 0;
        }

        if (_isReloading) return;

        bool reloadInput = false;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            reloadInput = true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            reloadInput = true;
        }

        if (reloadInput && _currentAmmo < MaxAmmo)
        {
            _reloadCoroutine = StartCoroutine(ReloadCoroutine());
            return;
        }

        float rtAxis = Gamepad.current != null ? Gamepad.current.rightTrigger.ReadValue() : 0f;
        bool rtHeld = rtAxis > _TRIGGER_DEADZONE;
        bool rtDown = rtHeld && !_wasRtPressedLastFrame;
        _wasRtPressedLastFrame = rtHeld;

        bool isMouseShootHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool isMouseShootDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        bool shootInput = (_data != null && _data.isAutomatic) ? (isMouseShootHeld || rtHeld) : (isMouseShootDown || rtDown);
        float fireRate = _data != null ? _data.fireRate : _DEFAULT_FIRE_RATE;

        if (shootInput && Time.time >= _nextTimeToFire)
        {
            if (_currentAmmo > 0)
            {
                _nextTimeToFire = Time.time + fireRate;
                Shoot();
            }
        }
    }

    public float GetCurrentSpread()
    {
        if (_data == null) return 0f;

        float totalSpread = _data.baseSpread + _firingSpreadPenalty;

        if (_playerController != null)
        {
            if (!_playerController.IsGrounded)
            {
                totalSpread += _data.airSpreadMultiplier;
            }
            else if (_playerController.IsMoving)
            {
                totalSpread += _data.movementSpreadMultiplier;
            }
        }

        return totalSpread;
    }

    private void Shoot()
    {
        _currentAmmo--;
        _lastShotTime = Time.time;

        if (_data != null)
        {
            _firingSpreadPenalty += _data.spreadPerShot * _SPREAD_BURST_MULTIPLIER;
        }

        PlayRandomShootSound();

        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_shootTriggerHash);
            _weaponAnimator.SetTrigger(_shootTriggerHash);
        }

        Camera cam = _playerController != null ? _playerController.PlayerCamera : null;
        if (cam == null) return;

        Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayOrigin = centerRay.origin;

        Vector2 recoilOffset = Vector2.zero;
        if (_data != null && _data.recoilPattern != null && _data.recoilPattern.Length > 0)
        {
            int index = Mathf.Min(_currentShotIndex, _data.recoilPattern.Length - 1);
            recoilOffset = _data.recoilPattern[index];
        }

        float currentSpread = GetCurrentSpread();
        bool isMoving = _playerController != null && _playerController.IsMoving;
        bool inAir = _playerController != null && !_playerController.IsGrounded;

        float minSpreadOffset = 0f;
        if (_data != null)
        {
            if (inAir)
            {
                minSpreadOffset = _data.airSpreadMultiplier * _AIR_SPREAD_FACTOR;
            }
            else if (isMoving)
            {
                minSpreadOffset = _data.movementSpreadMultiplier * _MOVE_SPREAD_FACTOR;
            }
        }

        _currentShotIndex++;

        float randomAngle = UnityEngine.Random.Range(0f, _TWO_PI);
        float baseRandomRadius = (UnityEngine.Random.value + UnityEngine.Random.value) * 0.5f;
        float randomRadius = Mathf.Lerp(minSpreadOffset, currentSpread, baseRandomRadius);

        Vector2 randomSpread = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
        float yawInDegrees = recoilOffset.x + randomSpread.x;
        float pitchInDegrees = recoilOffset.y + randomSpread.y;

        Quaternion spreadRotation = Quaternion.Euler(-pitchInDegrees, yawInDegrees, 0f);
        Vector3 finalDirection = cam.transform.rotation * spreadRotation * Vector3.forward;

        float weaponRange = _data != null ? _data.range : _DEFAULT_RANGE;
        Vector3 targetPoint = rayOrigin + finalDirection * weaponRange;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, finalDirection, weaponRange, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myRootTransform = cam.transform.root;

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
                float baseDamage = _data != null ? _data.damage : _DEFAULT_DAMAGE;
                hitTarget.ReceiveHit(baseDamage, NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                CreateImpactVisual(hit);
            }

            break;
        }

        _targetPosition += _kickbackOffset;
        _targetRotation *= Quaternion.Euler(_kickbackRotation);

        TriggerMuzzleFlash();

        Vector3 tracerStart = _firePoint != null ? _firePoint.position : rayOrigin;
        StartCoroutine(RenderTracer(tracerStart, targetPoint));
    }

    private void PlayRandomShootSound()
    {
        if (_data != null && _data.shootSounds != null && _data.shootSounds.Length > 0 && _audioSource != null)
        {
            int randomIndex = UnityEngine.Random.Range(0, _data.shootSounds.Length);
            AudioClip clip = _data.shootSounds[randomIndex];
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }

    public void CancelReload()
    {
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        _isReloading = false;

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

        if (_data != null && _data.reloadSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_data.reloadSound);
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
            _weaponAnimator.SetTrigger(_reloadTriggerHash);
        }

        float waitTime = _data != null ? _data.reloadTime : _DEFAULT_RELOAD_TIME;
        yield return new WaitForSeconds(waitTime);

        _currentAmmo = MaxAmmo;
        _isReloading = false;
        _reloadCoroutine = null;
        _currentShotIndex = 0;
        _firingSpreadPenalty = 0f;
    }

    private void TriggerMuzzleFlash()
    {
        if (_muzzleFlash == null) return;

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
        if (_data != null && _data.impactPrefabs != null && _data.impactPrefabs.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, _data.impactPrefabs.Length);
            GameObject selectedPrefab = _data.impactPrefabs[randomIndex];

            if (selectedPrefab != null)
            {
                Quaternion impactRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 180f, 0);
                GameObject impact = Instantiate(selectedPrefab, hit.point + hit.normal * 0.01f, impactRotation);
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
}