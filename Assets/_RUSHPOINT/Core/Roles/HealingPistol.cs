using Unity.Netcode;
using UnityEngine;

public class HealingPistol : WeaponBase
{
    private const float _DEFAULT_HEAL_AMOUNT = 35f;

    [SerializeField] private float _healAmount = _DEFAULT_HEAL_AMOUNT;
    [SerializeField] private ParticleSystem _healImpactEffect;

    protected override void Shoot()
    {
        base.Shoot();

        Camera playerCam = GetComponentInParent<NetworkPlayerController>()?.PlayerCamera;
        if (playerCam == null) return;

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float range = data != null ? data.range : 50f;

        if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
        {
            Transform hitRoot = hit.collider.transform.root;
            NetworkPlayerController targetPlayer = hitRoot.GetComponent<NetworkPlayerController>();

            if (targetPlayer != null)
            {
                NetworkHealth targetHealth = hitRoot.GetComponent<NetworkHealth>();
                if (targetHealth != null && targetHealth.IsAlive.Value)
                {
                    targetHealth.HealServerRpc(_healAmount);

                    if (_healImpactEffect != null)
                    {
                        Instantiate(_healImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                }
            }
        }
    }
}