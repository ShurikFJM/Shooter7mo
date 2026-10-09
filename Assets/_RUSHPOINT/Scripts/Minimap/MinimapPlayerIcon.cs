using UnityEngine;
using UnityEngine.UI;

public class MinimapPlayerIcon : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private RectTransform _minimapRect;
    [SerializeField] private RawImage _iconImage;
    [SerializeField] private float _minWorldX = -61.5f;
    [SerializeField] private float _maxWorldX = 102.64f;
    [SerializeField] private float _minWorldZ = 111f;
    [SerializeField] private float _maxWorldZ = -6.5f;
    [SerializeField] private bool _invertX = false;
    [SerializeField] private bool _invertZ = true;
    [SerializeField] private float _rotationOffset = 0f;

    private RectTransform _iconRect;
    private NetworkHealth _targetHealth;

    public Transform TargetPlayer => _player;

    private void Awake()
    {
        _iconRect = GetComponent<RectTransform>();
        if (_iconImage == null)
        {
            _iconImage = GetComponent<RawImage>();
        }
    }

    public void Setup(Transform target, RectTransform minimapRect, Color iconColor, float minX, float maxX, float minZ, float maxZ, bool invX, bool invZ, float rotOffset)
    {
        _player = target;
        _minimapRect = minimapRect;
        _minWorldX = minX;
        _maxWorldX = maxX;
        _minWorldZ = minZ;
        _maxWorldZ = maxZ;
        _invertX = invX;
        _invertZ = invZ;
        _rotationOffset = rotOffset;

        if (_iconImage == null)
        {
            _iconImage = GetComponent<RawImage>();
        }

        if (_iconImage != null)
        {
            _iconImage.color = iconColor;
        }

        if (_player != null)
        {
            _targetHealth = _player.GetComponent<NetworkHealth>();
        }
    }

    private void LateUpdate()
    {
        if (_player == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_targetHealth != null && !_targetHealth.IsAlive.Value)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }
        else if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (_minimapRect == null) return;

        UpdatePlayerPosition();
        UpdatePlayerRotation();
    }

    private void UpdatePlayerPosition()
    {
        float normalizedX = Mathf.InverseLerp(_minWorldX, _maxWorldX, _player.position.x);
        float normalizedZ = Mathf.InverseLerp(_minWorldZ, _maxWorldZ, _player.position.z);

        if (_invertX)
        {
            normalizedX = 1f - normalizedX;
        }

        if (_invertZ)
        {
            normalizedZ = 1f - normalizedZ;
        }

        float mapX = (normalizedX - 0.5f) * _minimapRect.rect.width;
        float mapY = (normalizedZ - 0.5f) * _minimapRect.rect.height;

        _iconRect.anchoredPosition = new Vector2(mapX, mapY);
    }

    private void UpdatePlayerRotation()
    {
        float rotation = -_player.eulerAngles.y + _rotationOffset;
        _iconRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }
}