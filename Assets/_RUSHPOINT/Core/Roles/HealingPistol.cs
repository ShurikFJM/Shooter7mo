using Unity.Netcode;
using UnityEngine;

public class HealingPistol : WeaponBase
{
    private const float DEFAULT_HEAL_AMOUNT = 35f;
    private const float DEFAULT_HEAL_RANGE = 50f;
    private const float VIEWPORT_CENTER_X = 0.5f;
    private const float VIEWPORT_CENTER_Y = 0.5f;

    [SerializeField] private float _healAmount = DEFAULT_HEAL_AMOUNT;
    [SerializeField] private float _healRange = DEFAULT_HEAL_RANGE;
    [SerializeField] private ParticleSystem _healImpactEffect;

    public void PerformHealShot()
    {
        if (!CanFire())
        {
            return;
        }

        Fire();

        Camera playerCameraInstance = GetComponentInParent<NetworkPlayerController>()?.PlayerCamera;
        if (playerCameraInstance == null)
        {
            return;
        }

        Ray aimRay = playerCameraInstance.ViewportPointToRay(new Vector3(VIEWPORT_CENTER_X, VIEWPORT_CENTER_Y, 0f));
        float range = _data != null ? _data.range : _healRange;

        if (Physics.Raycast(aimRay, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
        {
            Transform hitRoot = hit.collider.transform.root;
            NetworkPlayerController targetPlayer = hitRoot.GetComponent<NetworkPlayerController>();

            if (targetPlayer != null)
            {
                NetworkHealth targetHealth = hitRoot.GetComponent<NetworkHealth>();
                if (targetHealth != null && targetHealth.IsAlive.Value)
                {
                    targetHealth.HealServerRpc(_healAmount);
                    SpawnHealImpactEffectClientRpc(hit.point, hit.normal);
                }
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnHealImpactEffectClientRpc(Vector3 impactPoint, Vector3 surfaceNormal)
    {
        if (_healImpactEffect != null)
        {
            Instantiate(_healImpactEffect, impactPoint, Quaternion.LookRotation(surfaceNormal));
        }
    }
}