using Unity.Netcode;
using UnityEngine;

public class HealingPistol : WeaponBase
{
    [SerializeField] private float _healAmountAlly = 35f;
    [SerializeField] private float _healAmountSelf = 25f;
    [SerializeField] private float _selfHealCooldown = 1.0f;
    [SerializeField] private AudioClip _selfHealSound;
    [SerializeField] private GameObject _selfHealVfxPrefab;

    private float _nextTimeToSelfHeal;

    private void Update()
    {
        if (_playerController == null)
        {
            _playerController = GetComponentInParent<NetworkPlayerController>();
        }

        if (_playerController == null || !_playerController.IsOwner) return;

        HandleSelfHealInput();
    }

    private void HandleSelfHealInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase != RoundPhase.InProgress) return;
        if (_isReloading || _currentAmmo <= 0 || Time.time < _nextTimeToSelfHeal) return;

        bool isAimTriggered = InputManager.Instance != null && InputManager.Instance.IsAimPressed;

        if (isAimTriggered)
        {
            PerformSelfHeal();
        }
    }

    public override void Fire()
    {
        PerformHealShot();
    }

    public void PerformSelfHeal()
    {
        if (_playerController == null) _playerController = GetComponentInParent<NetworkPlayerController>();
        NetworkHealth localHealth = _playerController != null ? _playerController.GetComponent<NetworkHealth>() : null;
        if (localHealth == null || !localHealth.IsAlive.Value) return;

        if (localHealth.CurrentHealth.Value >= localHealth.MaxHealth.Value) return;

        _currentAmmo--;
        _nextTimeToSelfHeal = Time.time + _selfHealCooldown;
        _nextTimeToFire = Time.time + _selfHealCooldown;

        localHealth.HealServerRpc(_healAmountSelf);

        if (_selfHealSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_selfHealSound);
        }

        if (_selfHealVfxPrefab != null && _firePoint != null)
        {
            GameObject vfx = Instantiate(_selfHealVfxPrefab, _firePoint.position, _firePoint.rotation);
            Destroy(vfx, 1.5f);
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_shootTriggerHash);
        }

        if (_playerController != null)
        {
            _playerController.TriggerShootTPS();
        }
    }

    public void PerformHealShot()
    {
        if (!CanFire()) return;

        if (_playerController == null) _playerController = GetComponentInParent<NetworkPlayerController>();

        _currentAmmo--;
        float fireRate = weaponData != null && weaponData.fireRate > 0f ? weaponData.fireRate : 0.2f;
        _nextTimeToFire = Time.time + fireRate;

        PlayRandomShootSound();

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_shootTriggerHash);
        }

        if (_playerController != null)
        {
            _playerController.TriggerShootTPS();
        }

        Vector3 rayOrigin = _firePoint != null ? _firePoint.position : transform.position;
        Vector3 rayDirection = _firePoint != null ? _firePoint.forward : transform.forward;

        if (_playerController != null && _playerController.PlayerCamera != null)
        {
            rayOrigin = _playerController.PlayerCamera.transform.position;
            rayDirection = _playerController.PlayerCamera.transform.forward;
        }

        float maxRange = weaponData != null ? weaponData.range : 50f;
        Vector3 targetPoint = rayOrigin + (rayDirection * maxRange);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myRoot = _playerController != null ? _playerController.transform : transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.IsChildOf(myRoot) || hit.transform == myRoot) continue;
            if (hit.collider.isTrigger && hit.collider.GetComponent<Hitbox>() == null && hit.collider.GetComponentInParent<NetworkHealth>() == null) continue;

            targetPoint = hit.point;

            NetworkHealth victimHealth = hit.collider.GetComponentInParent<NetworkHealth>();
            PlayerTeam victimTeam = hit.collider.GetComponentInParent<PlayerTeam>();
            PlayerTeam myTeam = myRoot.GetComponent<PlayerTeam>();
            if (myTeam == null) myTeam = myRoot.GetComponentInChildren<PlayerTeam>(true);

            if (victimHealth != null && victimHealth.IsAlive.Value)
            {
                if (victimTeam != null && myTeam != null && victimTeam.CurrentTeam.Value == myTeam.CurrentTeam.Value)
                {
                    victimHealth.HealServerRpc(_healAmountAlly);
                    CreatePlayerImpactVisual(hit.point, hit.normal);
                    PlayImpactSound(hit.point, true);
                    break;
                }
            }

            CreateWallImpactVisual(hit.point, hit.normal);
            PlayImpactSound(hit.point, false);
            break;
        }

        CreateTracerVisual(targetPoint);
    }
}