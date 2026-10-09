using UnityEngine;
using UnityEngine.UI;

public class MinimapPlayerIcon : MonoBehaviour
{
    public RawImage iconImage;

    private Transform _targetPlayer;
    private MinimapManager _manager;
    private RectTransform _iconRect;
    private NetworkHealth _targetHealth;
    private float _rotationOffset;
    private bool _isTeammate;

    private void Awake()
    {
        _iconRect = GetComponent<RectTransform>();
        if (iconImage == null) iconImage = GetComponent<RawImage>();
    }

    public void Setup(Transform target, MinimapManager manager, Color color, float rotOffset, bool isTeammate)
    {
        _targetPlayer = target;
        _manager = manager;
        _rotationOffset = rotOffset;
        _isTeammate = isTeammate;

        if (iconImage != null) iconImage.color = color;
        if (_targetPlayer != null) _targetHealth = _targetPlayer.GetComponent<NetworkHealth>();
    }

    private void LateUpdate()
    {
        if (_targetPlayer == null || _manager == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_targetHealth != null && !_targetHealth.IsAlive.Value)
        {
            if (iconImage != null && iconImage.enabled) iconImage.enabled = false;
            return;
        }

        if (_isTeammate && iconImage != null && !iconImage.enabled)
        {
            iconImage.enabled = true;
        }

        UpdateIconTransform();
    }

    private void UpdateIconTransform()
    {
        Vector2 newAnchoredPosition = _manager.CalculateMinimapPosition(_targetPlayer.position);
        _iconRect.anchoredPosition = newAnchoredPosition;

        float rotation = -_targetPlayer.eulerAngles.y + _rotationOffset;
        _iconRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    public void SetVisibility(bool isVisible)
    {
        if (iconImage != null && iconImage.enabled != isVisible)
        {
            iconImage.enabled = isVisible;
        }
    }
}