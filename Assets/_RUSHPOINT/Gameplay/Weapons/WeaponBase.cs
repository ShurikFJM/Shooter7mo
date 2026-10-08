using Unity.Netcode;
using UnityEngine;

public class WeaponBase : NetworkBehaviour
{
    private const float DEFAULT_BASE_SPREAD = 1f;

    [SerializeField] protected WeaponData data;
    [SerializeField] private int _magazineCapacity = 30;
    [SerializeField] private int _maxReserveAmmo = 90;
    [SerializeField] private float _reloadDuration = 2.2f;
    [SerializeField] private float _baseSpread = DEFAULT_BASE_SPREAD;

    [SerializeField] private int _currentAmmo;
    [SerializeField] private int _reserveAmmo;
    [SerializeField] private bool _isReloading;
    [SerializeField] private float _reloadRemainingTime;

    public WeaponData WeaponData => data;
    public int CurrentAmmo => _currentAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public int MagazineCapacity => _magazineCapacity;
    public int MaxReserveAmmo => _maxReserveAmmo;
    public int MaxAmmo => _maxReserveAmmo > 0 ? _maxReserveAmmo : _magazineCapacity;
    public bool IsReloading => _isReloading;
    public float ReloadDuration => _reloadDuration;
    public float ReloadProgressNormalized => _reloadDuration > 0f ? Mathf.Clamp01(1f - (_reloadRemainingTime / _reloadDuration)) : 0f;

    protected virtual void Awake()
    {
        InitializeFromWeaponData();
    }

    private void Start()
    {
        InitializeFromWeaponData();
    }

    private void InitializeFromWeaponData()
    {
        if (data != null)
        {
            _magazineCapacity = data.maxAmmo > 0 ? data.maxAmmo : _magazineCapacity;
            _maxReserveAmmo = data.maxAmmo * data.maxReserveMagazines;
            _reloadDuration = data.reloadTime > 0f ? data.reloadTime : _reloadDuration;
            _baseSpread = data.baseSpread >= 0f ? data.baseSpread : _baseSpread;
        }

        if (_currentAmmo == 0 && _reserveAmmo == 0)
        {
            _currentAmmo = _magazineCapacity;
            _reserveAmmo = _maxReserveAmmo;
        }
    }

    protected virtual void Update()
    {
        if (_isReloading)
        {
            _reloadRemainingTime -= Time.deltaTime;
            if (_reloadRemainingTime <= 0f)
            {
                CompleteReload();
            }
        }
    }

    public virtual float GetCurrentSpread()
    {
        return _baseSpread;
    }

    public virtual bool CanFire()
    {
        return !_isReloading && _currentAmmo > 0;
    }

    public virtual void Fire()
    {
        if (!CanFire())
        {
            return;
        }

        _currentAmmo--;
        ExecuteWeaponFireEffectsClientRpc();
    }

    public virtual void Reload()
    {
        if (_isReloading || _currentAmmo >= _magazineCapacity || _reserveAmmo <= 0)
        {
            return;
        }

        _isReloading = true;
        _reloadRemainingTime = _reloadDuration;
        PlayReloadEffectsClientRpc();
    }

    private void CompleteReload()
    {
        int neededAmmo = _magazineCapacity - _currentAmmo;
        int ammoToTransfer = Mathf.Min(neededAmmo, _reserveAmmo);

        _currentAmmo += ammoToTransfer;
        _reserveAmmo -= ammoToTransfer;

        _isReloading = false;
        _reloadRemainingTime = 0f;
    }

    public void ResetAmmo()
    {
        InitializeFromWeaponData();
        _isReloading = false;
        _reloadRemainingTime = 0f;
        _currentAmmo = _magazineCapacity;
        _reserveAmmo = _maxReserveAmmo;
    }

    [Rpc(SendTo.ClientsAndHost)]
    protected virtual void ExecuteWeaponFireEffectsClientRpc()
    {
    }

    [Rpc(SendTo.ClientsAndHost)]
    protected virtual void PlayReloadEffectsClientRpc()
    {
    }
}