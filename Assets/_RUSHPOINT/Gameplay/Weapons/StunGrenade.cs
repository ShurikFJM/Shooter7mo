using Unity.Netcode;
using UnityEngine;

public class StunGrenade : NetworkGrenadeBase
{
    [Header("Configuración de Aturdimiento")]
    [SerializeField] private float _stunRadius = 12.0f;
    [SerializeField] private float _maxStunDuration = 4.0f;
    [SerializeField] private float _slowMultiplier = 0.4f;    
    [SerializeField] private float _sensMultiplier = 0.3f;    
    [SerializeField] private LayerMask _playerMask;
    [SerializeField] private LayerMask _occlusionMask;

    [Header("Efectos Audiovisuales")]
    [SerializeField] private GameObject _flashVfxPrefab;
    [SerializeField] private AudioClip _flashDetonationSound;

    protected override void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal)
    {
        Collider[] hits = Physics.OverlapSphere(impactPoint, _stunRadius, _playerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            NetworkPlayerController player = hits[i].GetComponentInParent<NetworkPlayerController>();
            if (player == null) continue;

            Vector3 targetHead = player.PlayerCamera != null ? player.PlayerCamera.transform.position : hits[i].bounds.center;
            Vector3 directionToTarget = targetHead - impactPoint;
            float distance = directionToTarget.magnitude;

            if (distance < 0.1f) distance = 0.1f;

            if (Physics.Raycast(impactPoint, directionToTarget.normalized, distance, _occlusionMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            float distanceRatio = Mathf.Clamp01(1f - (distance / _stunRadius));
            float finalDuration = Mathf.Lerp(_maxStunDuration * 0.4f, _maxStunDuration, distanceRatio);

            player.ApplyStunClientRpc(finalDuration, _slowMultiplier, _sensMultiplier);
        }
    }

    protected override void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (_flashVfxPrefab != null)
        {
            GameObject flashVfx = Instantiate(_flashVfxPrefab, impactPoint, Quaternion.identity);
            Destroy(flashVfx, 2.5f);
        }

        if (_audioSource != null && _flashDetonationSound != null)
        {
            _audioSource.PlayOneShot(_flashDetonationSound, 1.0f);
        }
    }
}