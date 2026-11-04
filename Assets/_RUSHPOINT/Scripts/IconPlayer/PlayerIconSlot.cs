using UnityEngine;
using Unity.Netcode;

public class PlayerIconSlot : MonoBehaviour
{
    [SerializeField] private GameObject _diamondRoot;
    [SerializeField] private GameObject _skullIcon;

    private NetworkHealth _boundHealth;

    public void Bind(NetworkHealth health)
    {
        Clear();

        _boundHealth = health;
        _diamondRoot.SetActive(true);

        if (_boundHealth != null)
        {
            _boundHealth.IsAlive.OnValueChanged += OnAliveChanged;

            UpdateVisuals(_boundHealth.IsAlive.Value);
        }
    }

    public void Clear()
    {
        if (_boundHealth != null)
        {
            _boundHealth.IsAlive.OnValueChanged -= OnAliveChanged;
            _boundHealth = null;
        }
        _diamondRoot.SetActive(false);
    }

    private void OnAliveChanged(bool previousValue, bool newValue)
    {
        UpdateVisuals(newValue);
    }

    private void UpdateVisuals(bool isAlive)
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