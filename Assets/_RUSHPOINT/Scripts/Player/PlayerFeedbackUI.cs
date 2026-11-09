using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;

[RequireComponent(typeof(AudioSource))] 
public class PlayerFeedbackUI : NetworkBehaviour
{
    [SerializeField] private float _delayBeforeFade = 2f;
    [SerializeField] private float _fadeSpeed = 1.5f;
    [SerializeField] private float _deathDuration = 4f;
    [SerializeField] private AudioClip _damageSound;
    [SerializeField] private AudioClip _deathSound;

    private NetworkHealth _health;
    private float _fadeTimer = 0f;
    private AudioSource _audioSource; 

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        _health = GetComponent<NetworkHealth>();
        _audioSource = GetComponent<AudioSource>();

        if (_health != null)
        {
            _health.CurrentHealth.OnValueChanged += HandleHealthChanged;
        }

        if (LocalFeedbackHUD.Instance != null)
        {
            if (LocalFeedbackHUD.Instance.DamageCanvasGroup != null)
            {
                LocalFeedbackHUD.Instance.DamageCanvasGroup.alpha = 0f;
                LocalFeedbackHUD.Instance.DamageCanvasGroup.gameObject.SetActive(true);
            }

            if (LocalFeedbackHUD.Instance.DeathPanel != null)
            {
                LocalFeedbackHUD.Instance.DeathPanel.SetActive(false);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (_health != null)
        {
            _health.CurrentHealth.OnValueChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(float previousValue, float currentValue)
    {
        if (currentValue < previousValue && currentValue > 0)
        {
            ShowDamageEffect();
        }
    }

    private void ShowDamageEffect()
    {
        if (LocalFeedbackHUD.Instance != null && LocalFeedbackHUD.Instance.DamageCanvasGroup != null)
        {
            LocalFeedbackHUD.Instance.DamageCanvasGroup.alpha = 1f;
            _fadeTimer = _delayBeforeFade;
        }

        if (_audioSource != null && _damageSound != null)
        {
            _audioSource.PlayOneShot(_damageSound);
        }
    }

    private void Update()
    {
        if (!IsOwner || LocalFeedbackHUD.Instance == null || LocalFeedbackHUD.Instance.DamageCanvasGroup == null) return;

        if (LocalFeedbackHUD.Instance.DamageCanvasGroup.alpha > 0)
        {
            if (_fadeTimer > 0)
            {
                _fadeTimer -= Time.deltaTime;
            }
            else
            {
                LocalFeedbackHUD.Instance.DamageCanvasGroup.alpha -= _fadeSpeed * Time.deltaTime;
            }
        }
    }

    [ClientRpc]
    public void ShowDeathScreenClientRpc(FixedString32Bytes killerName)
    {
        if (!IsOwner) return;
        StartCoroutine(DeathScreenRoutine(killerName.ToString()));
    }

    private IEnumerator DeathScreenRoutine(string killerName)
    {
        if (_audioSource != null && _deathSound != null)
        {
            _audioSource.PlayOneShot(_deathSound);
        }

        if (LocalFeedbackHUD.Instance != null)
        {
            if (LocalFeedbackHUD.Instance.DeathText != null)
            {
                LocalFeedbackHUD.Instance.DeathText.text = $"Haz muerto\nTe ha matado {killerName}";
            }

            if (LocalFeedbackHUD.Instance.DeathPanel != null)
            {
                LocalFeedbackHUD.Instance.DeathPanel.SetActive(true);
            }

            if (LocalFeedbackHUD.Instance.DamageCanvasGroup != null)
            {
                LocalFeedbackHUD.Instance.DamageCanvasGroup.alpha = 0f;
            }

            yield return new WaitForSeconds(_deathDuration);

            if (LocalFeedbackHUD.Instance.DeathPanel != null)
            {
                LocalFeedbackHUD.Instance.DeathPanel.SetActive(false);
            }
        }
    }
}