using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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
        if (_playerController == null || !_playerController.IsOwner) return;

        HandleSelfHealInput();
    }

    private void HandleSelfHealInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase != RoundPhase.InProgress) return;
        if (_isReloading || _currentAmmo <= 0 || Time.time < _nextTimeToSelfHeal) return;

        bool isAimTriggered = false;

        if (InputManager.Instance != null)
        {
            isAimTriggered = InputManager.Instance.IsAimPressed;
        }
        else if (Mouse.current != null)
        {
            isAimTriggered = Mouse.current.rightButton.isPressed;
        }

        if (Gamepad.current != null)
        {
            isAimTriggered = isAimTriggered || Gamepad.current.leftTrigger.isPressed;
        }

        if (isAimTriggered)
        {
            PerformSelfHeal();
        }
    }

    public void PerformSelfHeal()
    {
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
    }

    public void PerformHealShot()
    {
        if (!CanFire()) return;

        _currentAmmo--;
        float fireRate = weaponData != null && weaponData.fireRate > 0f ? weaponData.fireRate : 0.2f;
        _nextTimeToFire = Time.time + fireRate;

        PlayRandomShootSound();

        if (_weaponAnimator != null)
        {
            _weaponAnimator.SetTrigger(_shootTriggerHash);
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

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRange, _hitLayers != 0 ? _hitLayers : (LayerMask)~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform myRoot = _playerController != null ? _playerController.transform : transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.IsChildOf(myRoot) || hit.transform == myRoot) continue;
            if (hit.collider.isTrigger && hit.collider.GetComponent<Hitbox>() == null) continue;

            targetPoint = hit.point;

            NetworkHealth victimHealth = hit.collider.GetComponentInParent<NetworkHealth>();
            PlayerTeam victimTeam = hit.collider.GetComponentInParent<PlayerTeam>();
            PlayerTeam myTeam = _playerController != null ? _playerController.GetComponent<PlayerTeam>() : null;

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