using UnityEngine;
using UnityEngine.UI; 
using Unity.Netcode;

public class PlayerIconSlot : MonoBehaviour
{
    [SerializeField] private GameObject _squareRoot;
    [SerializeField] private GameObject _skullIcon;
    [SerializeField] private Image _healthFillImage;

    private NetworkHealth _boundHealth;

    public void Bind(NetworkHealth health)
    {
        Clear();

        _boundHealth = health;
        _squareRoot.SetActive(true);

        if (_boundHealth != null)
        {
            _boundHealth.CurrentHealth.OnValueChanged += OnHealthChanged;
            _boundHealth.IsAlive.OnValueChanged += OnAliveChanged;
            UpdateHealthVisual(_boundHealth.CurrentHealth.Value);
            UpdateAliveVisual(_boundHealth.IsAlive.Value);
        }
    }

    public void Clear()
    {
        if (_boundHealth != null)
        {
            _boundHealth.CurrentHealth.OnValueChanged -= OnHealthChanged;
            _boundHealth.IsAlive.OnValueChanged -= OnAliveChanged;
            _boundHealth = null;
        }
        _squareRoot.SetActive(false);
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        UpdateHealthVisual(newValue);
    }

    private void OnAliveChanged(bool previousValue, bool newValue)
    {
        UpdateAliveVisual(newValue);
    }

    private void UpdateHealthVisual(float currentHealth)
    {
        if (_healthFillImage != null && _boundHealth != null)
        {
            float maxHealth = _boundHealth.MaxHealth.Value > 0f ? _boundHealth.MaxHealth.Value : 100f;
            _healthFillImage.fillAmount = currentHealth / maxHealth;
        }
    }

    private void UpdateAliveVisual(bool isAlive)
    {
        if (_skullIcon != null)
        {
            _skullIcon.SetActive(!isAlive);
        }
    }

    private void OnDestroy()
    {
        Clear();
    }
}