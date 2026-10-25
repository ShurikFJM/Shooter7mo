using UnityEngine;

public class VFXAutoRecycle : MonoBehaviour
{
    private const float FALLBACK_LIFETIME = 1.5f;

    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private float _customLifetime = 0f;

    private float _timer;

    private void Awake()
    {
        if (_particleSystem == null)
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }
    }

    private void OnEnable()
    {
        _timer = 0f;

        if (_particleSystem != null)
        {
            _particleSystem.Clear();
            _particleSystem.Play();
        }
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        float maxLifetime = _customLifetime > 0f ? _customLifetime : (_particleSystem != null ? _particleSystem.main.duration : FALLBACK_LIFETIME);

        if (_timer >= maxLifetime)
        {
            gameObject.SetActive(false);
        }
    }

}