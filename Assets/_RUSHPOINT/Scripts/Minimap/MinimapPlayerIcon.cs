using UnityEngine;

public class MinimapPlayerIcon : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform _player;

    [Header("Minimap")]
    [SerializeField] private RectTransform _minimapRect;

    [Header("World Bounds")]
    [SerializeField] private float _minWorldX;
    [SerializeField] private float _maxWorldX;
    [SerializeField] private float _minWorldZ;
    [SerializeField] private float _maxWorldZ;

    [Header("Axis")]
    [SerializeField] private bool _invertX = false;
    [SerializeField] private bool _invertZ = true;

    [Header("Rotation")]
    [SerializeField] private float _rotationOffset = 0f;

    private RectTransform _iconRect;

    private void Awake()
    {
        _iconRect = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (_player == null || _minimapRect == null)
        {
            return;
        }

        UpdatePlayerPosition();
        UpdatePlayerRotation();
    }

    private void UpdatePlayerPosition()
    {
        float normalizedX = Mathf.InverseLerp(
            _minWorldX,
            _maxWorldX,
            _player.position.x
        );

        float normalizedZ = Mathf.InverseLerp(
            _minWorldZ,
            _maxWorldZ,
            _player.position.z
        );

        if (_invertX)
        {
            normalizedX = 1f - normalizedX;
        }

        if (_invertZ)
        {
            normalizedZ = 1f - normalizedZ;
        }

        float mapX =
            (normalizedX - 0.5f) *
            _minimapRect.rect.width;

        float mapY =
            (normalizedZ - 0.5f) *
            _minimapRect.rect.height;

        _iconRect.anchoredPosition = new Vector2(
            mapX,
            mapY
        );
    }

    private void UpdatePlayerRotation()
    {
        float rotation =
            -_player.eulerAngles.y +
            _rotationOffset;

        _iconRect.localRotation =
            Quaternion.Euler(0f, 0f, rotation);
    }
}
