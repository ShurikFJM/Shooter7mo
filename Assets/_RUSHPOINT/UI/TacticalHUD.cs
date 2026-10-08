using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TacticalHUD : MonoBehaviour
{
    private const string _NO_AMMO_STRING = "-";
    private const string _SLASH_STRING = " / ";
    private const string _RELOADING_LABEL = "RELOADING...";

    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _armorText;
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _currentAmmoText;
    [SerializeField] private TMP_Text _maxAmmoText;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _armorSlider;

    [SerializeField] private GameObject _reloadingPopupRoot;
    [SerializeField] private TMP_Text _reloadingPromptText;
    [SerializeField] private Image _reloadingProgressBar;

    public NetworkHealth PlayerHealth { get; set; }
    public WeaponInventory Inventory { get; set; }

    private void Awake()
    {
        HideReloadingPopup();
    }

    private void Update()
    {
        EnsureLocalPlayerReferences();
        UpdateHealthAndArmor();
        UpdateAmmoDisplay();
    }

    private void EnsureLocalPlayerReferences()
    {
        if (PlayerHealth != null && Inventory != null) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        if (PlayerHealth == null)
        {
            PlayerHealth = localPlayer.GetComponent<NetworkHealth>();
        }

        if (Inventory == null)
        {
            Inventory = localPlayer.GetComponent<WeaponInventory>();
            if (Inventory == null)
            {
                Inventory = localPlayer.GetComponentInChildren<WeaponInventory>(true);
            }
        }
    }

    private void UpdateHealthAndArmor()
    {
        if (PlayerHealth == null) return;

        if (_healthText != null)
        {
            _healthText.text = Mathf.CeilToInt(PlayerHealth.CurrentHealth.Value).ToString();
        }

        if (_armorText != null)
        {
            _armorText.text = Mathf.CeilToInt(PlayerHealth.CurrentArmor.Value).ToString();
        }

        if (_healthSlider != null)
        {
            _healthSlider.maxValue = PlayerHealth.MaxHealth.Value;
            _healthSlider.value = PlayerHealth.CurrentHealth.Value;
        }

        if (_armorSlider != null)
        {
            _armorSlider.maxValue = PlayerHealth.MaxArmor.Value;
            _armorSlider.value = PlayerHealth.CurrentArmor.Value;
        }
    }

    private void UpdateAmmoDisplay()
    {
        if (Inventory == null)
        {
            SetEmptyAmmoDisplay();
            HideReloadingPopup();
            return;
        }

        WeaponBase weapon = Inventory.ActiveWeapon;
        if (weapon == null)
        {
            SetEmptyAmmoDisplay();
            HideReloadingPopup();
            return;
        }

        if (weapon.IsReloading)
        {
            ShowReloadingPopup(weapon.ReloadProgressNormalized);

            if (_ammoText != null) _ammoText.text = _RELOADING_LABEL;
            if (_currentAmmoText != null) _currentAmmoText.text = _RELOADING_LABEL;
            if (_maxAmmoText != null) _maxAmmoText.text = weapon.ReserveAmmo.ToString();
            return;
        }

        HideReloadingPopup();

        string currentStr = weapon.CurrentAmmo.ToString();
        string reserveStr = weapon.ReserveAmmo.ToString();

        if (_ammoText != null)
        {
            _ammoText.text = currentStr + _SLASH_STRING + reserveStr;
        }

        if (_currentAmmoText != null)
        {
            _currentAmmoText.text = currentStr;
        }

        if (_maxAmmoText != null)
        {
            _maxAmmoText.text = reserveStr;
        }
    }

    private void ShowReloadingPopup(float progressNormalized)
    {
        if (_reloadingPopupRoot != null && !_reloadingPopupRoot.activeSelf)
        {
            _reloadingPopupRoot.SetActive(true);
        }

        if (_reloadingPromptText != null)
        {
            _reloadingPromptText.text = _RELOADING_LABEL;
        }

        if (_reloadingProgressBar != null)
        {
            _reloadingProgressBar.fillAmount = progressNormalized;
        }
    }

    private void HideReloadingPopup()
    {
        if (_reloadingPopupRoot != null && _reloadingPopupRoot.activeSelf)
        {
            _reloadingPopupRoot.SetActive(false);
        }
    }

    private void SetEmptyAmmoDisplay()
    {
        if (_ammoText != null)
        {
            _ammoText.text = _NO_AMMO_STRING + _SLASH_STRING + _NO_AMMO_STRING;
        }

        if (_currentAmmoText != null)
        {
            _currentAmmoText.text = _NO_AMMO_STRING;
        }

        if (_maxAmmoText != null)
        {
            _maxAmmoText.text = _NO_AMMO_STRING;
        }
    }
}