using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Salud")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Efectos de Impacto en Cabeza (Headshot)")]
    public GameObject headshotParticlePrefab;
    public AudioClip headshotSound;

    [Header("Efectos de Impacto Normal")]
    public GameObject bodyParticlePrefab;     
    public AudioClip bodySound;

    private AudioSource audioSource;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void TakeDamage(float damage, HitboxType hitboxType, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHealth -= damage;
        bool isHeadshot = (hitboxType == HitboxType.Head);

        SpawnImpactParticles(isHeadshot ? headshotParticlePrefab : bodyParticlePrefab, hitPoint, hitNormal);

        AudioClip clipToPlay = isHeadshot ? headshotSound : bodySound;
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }

        if (currentHealth <= 0f)
        {
            Die(isHeadshot);
        }
    }

    private void SpawnImpactParticles(GameObject prefab, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (prefab == null) return;

        Vector3 spawnPosition = hitPoint + (hitNormal * 0.02f);

        Quaternion spawnRotation = Quaternion.LookRotation(hitNormal);

        GameObject particleObj = Instantiate(prefab, spawnPosition, spawnRotation);

        ParticleSystem[] particleSystems = particleObj.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem ps in particleSystems)
        {
            var mainModule = ps.main;

            mainModule.loop = false;
            mainModule.prewarm = false;

            ps.Clear();
            ps.Play();
        }
        Destroy(particleObj, 2f);
    }

    void Die(bool wasHeadshot)
    {
        isDead = true;

        if (KillFeedHUD.Instance != null)
        {
            KillFeedHUD.Instance.TriggerKillNotification(wasHeadshot);
        }

        Destroy(gameObject);
    }
}