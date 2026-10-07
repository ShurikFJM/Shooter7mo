using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TacticalHUD : MonoBehaviour
{
    private const string _NO_AMMO_STRING = "-";
    private const string _SLASH_STRING = " / ";

    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _armorText;
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _currentAmmoText;
    [SerializeField] private TMP_Text _maxAmmoText;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _armorSlider;

    public NetworkHealth PlayerHealth { get; set; }
    public WeaponInventory Inventory { get; set; }

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
            _healthSlider.maxValue = PlayerHealth.MaxHealth;
            _healthSlider.value = PlayerHealth.CurrentHealth.Value;
        }

        if (_armorSlider != null)
        {
            _armorSlider.maxValue = PlayerHealth.MaxArmor;
            _armorSlider.value = PlayerHealth.CurrentArmor.Value;
        }
    }

    private void UpdateAmmoDisplay()
    {
        if (Inventory == null)
        {
            SetEmptyAmmoDisplay();
            return;
        }

        WeaponBase weapon = Inventory.ActiveWeapon;
        if (weapon == null)
        {
            SetEmptyAmmoDisplay();
            return;
        }

        string currentStr = weapon.CurrentAmmo.ToString();
        string maxStr = weapon.MaxAmmo.ToString();

        if (_ammoText != null)
        {
            _ammoText.text = currentStr + _SLASH_STRING + maxStr;
        }

        if (_currentAmmoText != null)
        {
            _currentAmmoText.text = currentStr;
        }

        if (_maxAmmoText != null)
        {
            _maxAmmoText.text = maxStr;
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