using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HealGrenade : NetworkGrenadeBase
{
    [SerializeField] private float _healRadius = 6.0f;
    [SerializeField] private float _cloudDuration = 7.0f;
    [SerializeField] private float _healPerTick = 12.0f;
    [SerializeField] private float _tickInterval = 0.5f;

    [SerializeField] private GameObject _healParticleCloudPrefab;
    [SerializeField] private AudioClip _healActivationSound;

    protected override void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal)
    {
        GameObject healZoneObj = new GameObject("ActiveHealZone");
        healZoneObj.transform.position = impactPoint;
        HealZoneRuntime zone = healZoneObj.AddComponent<HealZoneRuntime>();
        zone.Initialize(_healRadius, _cloudDuration, _healPerTick, _tickInterval, _throwerTeam);
    }

    protected override void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (_healParticleCloudPrefab != null)
        {
            Vector3 spawnPos = impactPoint + (Vector3.up * 0.1f);
            GameObject cloud = Instantiate(_healParticleCloudPrefab, spawnPos, Quaternion.identity);
            Destroy(cloud, _cloudDuration + 1f);
        }

        if (_healActivationSound != null)
        {
            AudioSource.PlayClipAtPoint(_healActivationSound, impactPoint, 1.0f);
        }
    }
}

public class HealZoneRuntime : MonoBehaviour
{
    private float _radius;
    private float _duration;
    private float _healAmount;
    private float _interval;
    private Team _team;
    private float _elapsed;

    public void Initialize(float radius, float duration, float healAmount, float interval, Team team)
    {
        _radius = radius;
        _duration = duration;
        _healAmount = healAmount;
        _interval = interval;
        _team = team;

        StartCoroutine(HealRoutine());
    }

    private IEnumerator HealRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_interval);

        while (_elapsed < _duration)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
            for (int i = 0; i < hits.Length; i++)
            {
                NetworkHealth health = hits[i].GetComponentInParent<NetworkHealth>();
                if (health != null && health.IsAlive.Value)
                {
                    PlayerTeam playerTeam = health.GetComponent<PlayerTeam>();
                    if (playerTeam == null) playerTeam = health.GetComponentInChildren<PlayerTeam>();

                    if (playerTeam != null && (_team == Team.Neutral || playerTeam.CurrentTeam.Value == _team))
                    {
                        health.HealServer(_healAmount);
                    }
                }
            }

            _elapsed += _interval;
            yield return wait;
        }

        Destroy(gameObject);
    }
}