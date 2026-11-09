using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StunVisualEffect : MonoBehaviour
{
    public static StunVisualEffect Instance { get; private set; }

    [Header("Componentes Visuales")]
    [SerializeField] private CanvasGroup _flashCanvasGroup;
    [SerializeField] private Image _flashImage;

    [Header("Audio (Tinnitus / Pitido)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _tinnitusSound;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_flashCanvasGroup == null)
            _flashCanvasGroup = GetComponent<CanvasGroup>();

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        ResetEffect();
    }

    public void TriggerStun(float duration)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        if (_flashCanvasGroup != null)
            _flashCanvasGroup.alpha = 1.0f;

        if (_audioSource != null && _tinnitusSound != null)
        {
            _audioSource.clip = _tinnitusSound;
            _audioSource.volume = 1.0f;
            _audioSource.Play();
        }

        float solidTime = duration * 0.25f;
        yield return new WaitForSeconds(solidTime);

        float fadeTime = duration - solidTime;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            float alpha = Mathf.Lerp(1.0f, 0.0f, t * t);

            if (_flashCanvasGroup != null)
                _flashCanvasGroup.alpha = alpha;

            if (_audioSource != null)
                _audioSource.volume = alpha;

            yield return null;
        }

        ResetEffect();
    }

    private void ResetEffect()
    {
        if (_flashCanvasGroup != null)
            _flashCanvasGroup.alpha = 0f;

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }
}