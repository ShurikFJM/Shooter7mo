using Unity.Netcode;
using UnityEngine;

public class FragGrenade : NetworkGrenadeBase
{
    [SerializeField] private float _explosionRadius = 7.5f;
    [SerializeField] private float _maxDamage = 115f;
    [SerializeField] private float _minDamage = 25f;
    [SerializeField] private float _explosionForce = 600f;
    [SerializeField] private LayerMask _damageMask = ~0;
    [SerializeField] private LayerMask _occlusionMask;

    [SerializeField] private GameObject _explosionVfxPrefab;
    [SerializeField] private AudioClip _explosionSound;

    protected override void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal)
    {
        Collider[] colliders = Physics.OverlapSphere(impactPoint, _explosionRadius, _damageMask, QueryTriggerInteraction.Ignore);
        System.Collections.Generic.HashSet<NetworkHealth> damagedEntities = new System.Collections.Generic.HashSet<NetworkHealth>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            NetworkHealth health = col.GetComponentInParent<NetworkHealth>();

            if (health != null && !damagedEntities.Contains(health))
            {
                damagedEntities.Add(health);

                Vector3 targetHeadOrCenter = col.bounds.center;
                Vector3 directionToTarget = targetHeadOrCenter - impactPoint;
                float distance = directionToTarget.magnitude;

                if (Physics.Raycast(impactPoint, directionToTarget.normalized, out RaycastHit hit, distance, _occlusionMask))
                {
                    if (hit.collider != col && !hit.collider.transform.IsChildOf(health.transform))
                    {
                        continue;
                    }
                }

                PlayerTeam victimTeam = health.GetComponent<PlayerTeam>();
                if (victimTeam == null) victimTeam = health.GetComponentInChildren<PlayerTeam>();


                float distanceRatio = Mathf.Clamp01(distance / _explosionRadius);
                float damage = Mathf.Lerp(_maxDamage, _minDamage, distanceRatio);

                health.ApplyDamageServer(damage, HitboxType.Chest, _throwerClientId);
            }

            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddExplosionForce(_explosionForce, impactPoint, _explosionRadius, 1.2f, ForceMode.Impulse);
            }
        }
    }

    protected override void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (_explosionVfxPrefab != null)
        {
            Quaternion rot = impactNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(impactNormal) : Quaternion.identity;
            GameObject vfx = Instantiate(_explosionVfxPrefab, impactPoint, rot);
            Destroy(vfx, 4.0f);
        }

        if (_explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(_explosionSound, impactPoint, 1.0f);
        }
    }
}