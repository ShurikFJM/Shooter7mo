using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTarget : MonoBehaviour
{
    [Header("Target Health")]
    [SerializeField] private float _maxHealth = 50f;

    [Header("Feedback Effects")]
    [SerializeField] private GameObject _hitParticles;
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _destroySound;

    private float _currentHealth;
    private bool _isDestroyed;
    private AudioSource _audioSource;

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.spatialBlend = 1f;
        _audioSource.playOnAwake = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (_isDestroyed) return;

        _currentHealth -= damageAmount;

        if (_hitSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_hitSound);
        }

        if (_currentHealth <= 0f)
        {
            DestroyTarget();
        }
    }

    private void DestroyTarget()
    {
        _isDestroyed = true;

        if (_destroySound != null)
        {
            AudioSource.PlayClipAtPoint(_destroySound, transform.position);
        }

        if (_hitParticles != null)
        {
            Instantiate(_hitParticles, transform.position, Quaternion.identity);
        }

        TutorialManager.Instance?.OnTargetDestroyed();
        gameObject.SetActive(false);
    }
}