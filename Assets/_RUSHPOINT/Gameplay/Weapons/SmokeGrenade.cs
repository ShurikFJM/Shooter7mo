using Unity.Netcode;
using UnityEngine;

public class SmokeGrenade : NetworkGrenadeBase
{
    [SerializeField] private float _smokeDuration = 14f;

    [SerializeField] private GameObject _smokeParticlePrefab;
    [SerializeField] private AudioClip _smokeHissSound;

    protected override void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal)
    {
        SpawnSmokeCloudClientRpc(impactPoint);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnSmokeCloudClientRpc(Vector3 spawnPosition)
    {
        if (_smokeParticlePrefab != null)
        {
            GameObject smokeCloud = Instantiate(_smokeParticlePrefab, spawnPosition, Quaternion.identity);
            Destroy(smokeCloud, _smokeDuration + 1.5f);
        }

        if (_audioSource != null && _smokeHissSound != null)
        {
            AudioSource.PlayClipAtPoint(_smokeHissSound, spawnPosition, 1.0f);
        }
    }

    protected override void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal)
    {

    }
}