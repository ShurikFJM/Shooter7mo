using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [SerializeField] private RectTransform _topTick;
    [SerializeField] private RectTransform _bottomTick;
    [SerializeField] private RectTransform _leftTick;
    [SerializeField] private RectTransform _rightTick;

    [SerializeField] private float _baseGap = 12f;
    [SerializeField] private float _gapScaleMultiplier = 25f;
    [SerializeField] private float _maxGap = 160f;
    [SerializeField] private float _expandSpeed = 35f;
    [SerializeField] private float _contractSpeed = 20f;

    [SerializeField] private WeaponInventory _inventory;

    private float _currentGap;

    public WeaponInventory Inventory
    {
        get => _inventory;
        set => _inventory = value;
    }

    private void Start()
    {
        if (_inventory == null)
        {
            _inventory = FindAnyObjectByType<WeaponInventory>();
        }

        _currentGap = _baseGap;
    }

    private void Update()
    {
        if (_inventory == null || _inventory.ActiveWeapon == null) return;

        WeaponBase activeWeapon = _inventory.ActiveWeapon;

        float realTimeSpread = activeWeapon.GetCurrentSpread();
        float targetGap = _baseGap + (realTimeSpread * _gapScaleMultiplier);

        if (activeWeapon.IsReloading)
        {
            targetGap = _baseGap;
        }

        targetGap = Mathf.Clamp(targetGap, _baseGap, _maxGap);

        float lerpSpeed = (targetGap > _currentGap) ? _expandSpeed : _contractSpeed;
        _currentGap = Mathf.Lerp(_currentGap, targetGap, Time.deltaTime * lerpSpeed);

        if (_topTick != null) _topTick.anchoredPosition = new Vector2(0f, _currentGap);
        if (_bottomTick != null) _bottomTick.anchoredPosition = new Vector2(0f, -_currentGap);
        if (_leftTick != null) _leftTick.anchoredPosition = new Vector2(-_currentGap, 0f);
        if (_rightTick != null) _rightTick.anchoredPosition = new Vector2(_currentGap, 0f);
    }
}