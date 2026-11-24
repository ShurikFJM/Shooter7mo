using System.Collections.Generic;
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
        Collider[] colliders = Physics.OverlapSphere(impactPoint, _stunRadius, _targetMask, QueryTriggerInteraction.Collide);
        HashSet<NetworkPlayerController> affectedPlayers = new HashSet<NetworkPlayerController>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null) continue;

            NetworkPlayerController player = col.GetComponentInParent<NetworkPlayerController>();
            if (player == null || affectedPlayers.Contains(player)) continue;

            affectedPlayers.Add(player);

            Vector3 targetEyePos = player.PlayerCamera != null
                ? player.PlayerCamera.transform.position
                : player.transform.position + Vector3.up * 1.5f;

            Vector3 origin = impactPoint + (impactNormal * 0.15f);
            Vector3 directionToTarget = targetEyePos - origin;
            float distance = directionToTarget.magnitude;

            if (distance > 0.05f && _occlusionMask.value != 0)
            {
                RaycastHit[] hits = Physics.RaycastAll(origin, directionToTarget.normalized, distance, _occlusionMask, QueryTriggerInteraction.Ignore);
                bool blockedByWall = false;

                for (int h = 0; h < hits.Length; h++)
                {
                    Collider hitCol = hits[h].collider;
                    if (hitCol == null) continue;
                    if (hitCol.transform.IsChildOf(transform)) continue;
                    if (hitCol.transform.IsChildOf(player.transform)) continue;

                    blockedByWall = true;
                    break;
                }

                if (blockedByWall) continue;
            }

            player.ApplyStunClientRpc(_stunDuration, _slowMultiplier, _sensitivityMultiplier);
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