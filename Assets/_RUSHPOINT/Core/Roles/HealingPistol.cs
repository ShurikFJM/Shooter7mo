using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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
            // Tutorial: si el disparo toca al dummy de entrenamiento, el paso se da por completado.
            if (TutorialManager.Instance != null && TutorialManager.Instance.OnTrainingDummyHit(hit.collider))
            {
                if (_healImpactEffect != null)
                {
                    Instantiate(_healImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                }

                return;
            }

            Transform hitRoot = hit.collider.transform.root;
            NetworkPlayerController targetPlayer = hitRoot.GetComponent<NetworkPlayerController>();
            PlayerTeam targetTeam = hitRoot.GetComponent<PlayerTeam>();
            PlayerTeam myTeam = GetComponentInParent<PlayerTeam>();

            if (targetPlayer != null && targetTeam != null && myTeam != null)
            {
                if (targetTeam.CurrentTeam.Value == myTeam.CurrentTeam.Value)
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
}