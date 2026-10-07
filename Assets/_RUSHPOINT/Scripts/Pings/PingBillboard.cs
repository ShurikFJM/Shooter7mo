using UnityEngine;
using TMPro; 

public class PingBillboard : MonoBehaviour
{
    [SerializeField] private float _lifetime = 4f;
    [SerializeField] private MeshRenderer _quadRenderer;
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _dangerMaterial;
    [SerializeField] private Material _groupMaterial;
    [SerializeField] private TextMeshPro _distanceTextMeshPro;
    [SerializeField] private bool _enableAutoScaling = true;
    [SerializeField] private float _scaleFactor = 0.08f;
    [SerializeField] private float _minScale = 0.3f;
    [SerializeField] private float _maxScale = 3.0f;

    private Camera _targetCamera;

    public void Initialize(PlayerPingSystem.PingType pingType, float initialDistance, Camera playerCamera = null)
    {
        _targetCamera = playerCamera != null ? playerCamera : Camera.main;
        if (_quadRenderer != null)
        {
            switch (pingType)
            {
                case PlayerPingSystem.PingType.Normal:
                    if (_normalMaterial != null) _quadRenderer.material = _normalMaterial;
                    break;

                case PlayerPingSystem.PingType.Danger:
                    if (_dangerMaterial != null) _quadRenderer.material = _dangerMaterial;
                    break;

                case PlayerPingSystem.PingType.Group:
                    if (_groupMaterial != null) _quadRenderer.material = _groupMaterial;
                    break;
            }
        }

        if (_distanceTextMeshPro != null)
        {
            _distanceTextMeshPro.text = $"{Mathf.RoundToInt(initialDistance)}m";
        }

        Destroy(gameObject, _lifetime);
    }

    private void LateUpdate()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
            if (_targetCamera == null) return;
        }
        transform.rotation = _targetCamera.transform.rotation;
        if (_enableAutoScaling)
        {
            float dist = Vector3.Distance(transform.position, _targetCamera.transform.position);
            float calculatedScale = Mathf.Clamp(dist * _scaleFactor, _minScale, _maxScale);
            transform.localScale = new Vector3(calculatedScale, calculatedScale, calculatedScale);

            if (_distanceTextMeshPro != null)
            {
                _distanceTextMeshPro.text = $"{Mathf.RoundToInt(dist)}m";
            }
        }
    }
}