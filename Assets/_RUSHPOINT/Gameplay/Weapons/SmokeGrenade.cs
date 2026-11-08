using Unity.Netcode;
using UnityEngine;

public class SmokeGrenade : NetworkGrenadeBase
{
    [SerializeField] private float _smokeDuration = 14f;
    [SerializeField] private GameObject _smokeParticlePrefab;
    [SerializeField] private AudioClip _smokeHissSound;

    protected override void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal)
    {
    }

    protected override void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (_smokeParticlePrefab != null)
        {
            Vector3 spawnPos = impactPoint + (Vector3.up * 0.2f);
            GameObject smokeCloud = Instantiate(_smokeParticlePrefab, spawnPos, Quaternion.identity);

            ParticleSystem ps = smokeCloud.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
            }

            AudioSource audio = smokeCloud.GetComponent<AudioSource>();
            if (audio == null) audio = smokeCloud.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.minDistance = 3f;
            audio.maxDistance = 25f;

            if (_smokeHissSound != null)
            {
                audio.clip = _smokeHissSound;
                audio.loop = false;
                audio.Play();
            }

            Destroy(smokeCloud, _smokeDuration);
        }
    }
}