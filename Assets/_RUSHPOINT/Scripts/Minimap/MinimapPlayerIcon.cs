using Unity.Netcode;
using UnityEngine;

public class MinimapPlayerIcon : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private RectTransform _minimapRect;
    [SerializeField] private float _minWorldX = -50f;
    [SerializeField] private float _maxWorldX = 50f;
    [SerializeField] private float _minWorldZ = -50f;
    [SerializeField] private float _maxWorldZ = 50f;
    [SerializeField] private bool _invertX = false;
    [SerializeField] private bool _invertZ = true;
    [SerializeField] private float _rotationOffset = 0f;

    private RectTransform _iconRect;

    private void Awake()
    {
        _iconRect = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (_player == null)
        {
            ResolveLocalPlayer();
            if (_player == null) return;
        }

        if (_minimapRect == null) return;

        UpdatePlayerPosition();
        UpdatePlayerRotation();
    }

    private void ResolveLocalPlayer()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
        {
            _player = localPlayer.transform;
        }
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