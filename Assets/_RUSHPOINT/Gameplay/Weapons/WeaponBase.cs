using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WeaponBase : NetworkBehaviour
{
    public WeaponDataSO weaponData;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTracerPrefab;
    public Transform hitParticlesPrefab;

    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private Transform _weaponModelTransform;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private LayerMask _hitLayers;

    private NetworkPlayerController _playerController;
    private Vector3 _defaultLocalPosition;
    private Quaternion _defaultLocalRotation;
    private Vector3 _targetOffsetPosition;
    private Quaternion _targetOffsetRotation;

    private int _reloadTriggerHash = Animator.StringToHash("Reload");
    private int _shootTriggerHash = Animator.StringToHash("Shoot");

    private bool _isReloading;
    private float _reloadRemainingTime;
    private Coroutine _reloadCoroutine;
    private int _continuousShots;
    private float _firingSpreadPenalty;
    private float _nextFireTime;
    private int _currentAmmo;

    private void Awake()
    {
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

        _isReloading = false;
        _reloadRemainingTime = 0f;
        _reloadCoroutine = null;
        _continuousShots = 0;
        _firingSpreadPenalty = 0f;

        _targetOffsetPosition = Vector3.zero;
        _targetOffsetRotation = Quaternion.identity;

        if (_weaponModelTransform != null)
        {
            _weaponModelTransform.localPosition = _defaultLocalPosition;
            _weaponModelTransform.localRotation = _defaultLocalRotation;
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
            _weaponAnimator.Rebind();
            _weaponAnimator.Play("Idle", 0, 0f);
            _weaponAnimator.Update(0f);
        }
    }

    public bool CanFire()
    {
        if (_isReloading) return false;
        if (Time.time < _nextFireTime) return false;
        if (_currentAmmo <= 0) return false;
        return true;
    }

    public virtual void Fire()
    {
        if (!CanFire()) return;

        _nextFireTime = Time.time + (1f / weaponData.fireRate);
        _currentAmmo--;
        _continuousShots++;

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_shootTriggerHash);
        }

        if (muzzleFlashPrefab != null && _firePoint != null)
        {
            Instantiate(muzzleFlashPrefab, _firePoint.position, _firePoint.rotation, _firePoint);
        }

        ExecuteRaycast();
    }

    private void ExecuteRaycast()
    {
        if (_playerController == null || _playerController.PlayerCamera == null) return;

        Transform cameraTransform = _playerController.PlayerCamera.transform;
        Vector3 rayOrigin = cameraTransform.position;
        Vector3 rayDirection = cameraTransform.forward;
        Vector3 targetPoint;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, weaponData.maxRange, _hitLayers))
        {
            targetPoint = hit.point;

            NetworkHealth targetHealth = hit.collider.GetComponent<NetworkHealth>();
            if (targetHealth != null && IsOwner)
            {
                targetHealth.TakeDamageServerRpc(weaponData.baseDamage);
            }

            if (hitParticlesPrefab != null)
            {
                Instantiate(hitParticlesPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            targetPoint = rayOrigin + (rayDirection * weaponData.maxRange);
        }

        if (bulletTracerPrefab != null && _firePoint != null)
        {
            GameObject tracer = Instantiate(bulletTracerPrefab, _firePoint.position, Quaternion.identity);
            tracer.transform.LookAt(targetPoint);
        }
    }

    public void Reload()
    {
        if (_isReloading || _currentAmmo == weaponData.magazineSize) return;

        _isReloading = true;

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_reloadTriggerHash);
        }

        _reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _reloadRemainingTime = weaponData.reloadTime;

        while (_reloadRemainingTime > 0)
        {
            _reloadRemainingTime -= Time.deltaTime;
            yield return null;
        }

        _currentAmmo = weaponData.magazineSize;
        _isReloading = false;
        _continuousShots = 0;
        _firingSpreadPenalty = 0f;
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

        if (_weaponAnimator != null && gameObject.activeInHierarchy)
        {
            _weaponAnimator.ResetTrigger(_reloadTriggerHash);
            _weaponAnimator.Rebind();
            _weaponAnimator.Update(0f);
        }
    }
}