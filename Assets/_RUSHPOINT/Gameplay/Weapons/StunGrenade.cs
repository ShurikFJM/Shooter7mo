using Unity.Netcode;
using UnityEngine;

public class StunGrenade : NetworkGrenadeBase
{
    [SerializeField] private float _stunRadius = 9.0f;
    [SerializeField] private float _stunDuration = 3.0f;
    [SerializeField] private float _slowMultiplier = 0.45f;
    [SerializeField] private float _sensitivityMultiplier = 0.4f;
    [SerializeField] private LayerMask _targetMask = ~0;
    [SerializeField] private LayerMask _occlusionMask;

    [SerializeField] private GameObject _stunVfxPrefab;
    [SerializeField] private AudioClip _stunExplosionSound;

    protected override void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal)
    {
        Collider[] colliders = Physics.OverlapSphere(impactPoint, _stunRadius, _targetMask, QueryTriggerInteraction.Ignore);
        System.Collections.Generic.HashSet<NetworkPlayerController> affectedPlayers = new System.Collections.Generic.HashSet<NetworkPlayerController>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            NetworkPlayerController player = col.GetComponentInParent<NetworkPlayerController>();

            if (player != null && !affectedPlayers.Contains(player))
            {
                affectedPlayers.Add(player);

                Vector3 targetCenter = col.bounds.center;
                Vector3 directionToTarget = targetCenter - impactPoint;
                float distance = directionToTarget.magnitude;

                if (Physics.Raycast(impactPoint, directionToTarget.normalized, out RaycastHit hit, distance, _occlusionMask))
                {
                    if (hit.collider != col && !hit.collider.transform.IsChildOf(player.transform))
                    {
                        continue;
                    }
                }

                player.ApplyStunClientRpc(_stunDuration, _slowMultiplier, _sensitivityMultiplier);
            }
        }
    }

    protected override void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (_stunVfxPrefab != null)
        {
            Quaternion rot = impactNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(impactNormal) : Quaternion.identity;
            GameObject vfx = Instantiate(_stunVfxPrefab, impactPoint, rot);
            Destroy(vfx, 3.5f);
        }

        if (_stunExplosionSound != null)
        {
            AudioSource.PlayClipAtPoint(_stunExplosionSound, impactPoint, 1.2f);
        }
    }
}