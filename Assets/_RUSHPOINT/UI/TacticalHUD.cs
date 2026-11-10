using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TacticalHUD : MonoBehaviour
{
    private const string NO_AMMO_STRING = "-";
    private const string SLASH_STRING = " / ";
    private const string RELOADING_LABEL = "Reloading";
    private const string DEFAULT_HALFTIME_MESSAGE = "SIDE SWITCH!\n<size=60%>SWITCHING SIDES</size>";
    private const float DEFAULT_BANNER_DURATION = 4.5f;

    private static TacticalHUD _instance;
    public static TacticalHUD Instance => _instance;

    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _armorText;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _armorSlider;

    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _currentAmmoText;
    [SerializeField] private TMP_Text _maxAmmoText;

    [SerializeField] private GameObject _reloadingPopupRoot;
    [SerializeField] private TMP_Text _reloadingPromptText;
    [SerializeField] private Image _reloadingProgressBar;

    [SerializeField] private GameObject _halftimeBannerRoot;
    [SerializeField] private TMP_Text _halftimeBannerText;
    [SerializeField] private float _bannerDisplayDuration = DEFAULT_BANNER_DURATION;

    private NetworkHealth _playerHealth;
    public NetworkHealth PlayerHealth
    {
        get => _playerHealth;
        set => _playerHealth = value;
    }

    private WeaponInventory _weaponInventory;
    public WeaponInventory Inventory
    {
        get => _weaponInventory;
        set => _weaponInventory = value;
    }

    public WeaponInventory WeaponInventory
    {
        get => _weaponInventory;
        set => _weaponInventory = value;
    }

    private Coroutine _bannerCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        HideReloadingPopup();
        HideBanner();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        EnsureLocalPlayerReferences();
        UpdateHealthAndArmor();
        UpdateAmmoDisplay();
    }

    public void ShowHalftimeBanner()
    {
        DisplayCustomBanner(DEFAULT_HALFTIME_MESSAGE, _bannerDisplayDuration);
    }

    public void ShowRoundWinnerBanner(Team winningTeam)
    {
        string teamLabel = winningTeam == Team.Red ? "ATTACKERS" : "DEFENDERS";
        string message = $"{teamLabel} WON THE ROUND";
        DisplayCustomBanner(message, 3.5f);
    }

    public void ShowMatchWinnerBanner(Team winningTeam)
    {
        string teamLabel = winningTeam == Team.Red ? "ATTACKERS" : "DEFENDERS";
        Team localTeam = ResolveLocalTeam();

        string bannerMessage;
        if (localTeam == Team.Neutral)
        {
            bannerMessage = $"{teamLabel} WON THE MATCH!\n<size=60%>MATCH FINISHED</size>";
        }
        else if (localTeam == winningTeam)
        {
            bannerMessage = $"VICTORY!\n<size=60%>{teamLabel} WON THE MATCH</size>";
        }
        else
        {
            bannerMessage = $"DEFEAT!\n<size=60%>{teamLabel} WON THE MATCH</size>";
        }

        DisplayCustomBanner(bannerMessage, 8f);
    }

    private Team ResolveLocalTeam()
    {
        if (_playerHealth != null)
        {
            PlayerTeam playerTeam = _playerHealth.GetComponent<PlayerTeam>();
            if (playerTeam == null) playerTeam = _playerHealth.GetComponentInChildren<PlayerTeam>();
            if (playerTeam != null) return playerTeam.CurrentTeam.Value;
        }

        if (NetworkManager.Singleton != null && CustomLobbyManager.SavedTeams != null)
        {
            if (CustomLobbyManager.SavedTeams.TryGetValue(NetworkManager.Singleton.LocalClientId, out Team saved))
            {
                return saved;
            }
        }

        return Team.Neutral;
    }

    public void DisplayCustomBanner(string message, float duration)
    {
        if (_bannerCoroutine != null)
        {
            StopCoroutine(_bannerCoroutine);
        }

        _bannerCoroutine = StartCoroutine(BannerRoutine(message, duration));
    }

    private IEnumerator BannerRoutine(string message, float duration)
    {
        if (_halftimeBannerRoot != null)
        {
            _halftimeBannerRoot.SetActive(true);
        }

        if (_halftimeBannerText != null)
        {
            _halftimeBannerText.text = message;
        }

        yield return new WaitForSeconds(duration);

        HideBanner();
        _bannerCoroutine = null;
    }

    private void HideBanner()
    {
        if (_halftimeBannerRoot != null && _halftimeBannerRoot.activeSelf)
        {
            _halftimeBannerRoot.SetActive(false);
        }
    }

    private void EnsureLocalPlayerReferences()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        if (_playerHealth == null || _playerHealth.gameObject != localPlayer.gameObject)
        {
            _playerHealth = localPlayer.GetComponent<NetworkHealth>();
            if (_playerHealth == null)
            {
                _playerHealth = localPlayer.GetComponentInChildren<NetworkHealth>(true);
            }
        }

        if (_weaponInventory == null || _weaponInventory.gameObject != localPlayer.gameObject)
        {
            _weaponInventory = localPlayer.GetComponent<WeaponInventory>();
            if (_weaponInventory == null)
            {
                _weaponInventory = localPlayer.GetComponentInChildren<WeaponInventory>(true);
            }
        }
    }

    private void UpdateHealthAndArmor()
    {
        if (_playerHealth == null) return;

        if (_healthText != null)
        {
            _healthText.text = Mathf.CeilToInt(_playerHealth.CurrentHealth.Value).ToString();
        }

        if (_armorText != null)
        {
            _armorText.text = Mathf.CeilToInt(_playerHealth.CurrentArmor.Value).ToString();
        }

        if (_healthSlider != null)
        {
            _healthSlider.maxValue = _playerHealth.MaxHealth.Value;
            _healthSlider.value = _playerHealth.CurrentHealth.Value;
        }

        if (_armorSlider != null)
        {
            _armorSlider.maxValue = _playerHealth.MaxArmor.Value;
            _armorSlider.value = _playerHealth.CurrentArmor.Value;
        }
    }

    private void UpdateAmmoDisplay()
    {
        if (_weaponInventory == null)
        {
            SetEmptyAmmoDisplay();
            HideReloadingPopup();
            return;
        }

        if (_weaponInventory.ActiveSlotIndex == 3)
        {
            SetEmptyAmmoDisplay();
            HideReloadingPopup();
            return;
        }

        WeaponBase activeWeapon = _weaponInventory.ActiveWeapon;
        if (activeWeapon == null)
        {
            SetEmptyAmmoDisplay();
            HideReloadingPopup();
            return;
        }

        if (activeWeapon.IsReloading)
        {
            ShowReloadingPopup(activeWeapon.ReloadProgressNormalized);

            if (_ammoText != null) _ammoText.text = RELOADING_LABEL;
            if (_currentAmmoText != null) _currentAmmoText.text = RELOADING_LABEL;
            if (_maxAmmoText != null) _maxAmmoText.text = activeWeapon.ReserveAmmo.ToString();
            return;
        }

        HideReloadingPopup();

        string currentAmmoString = activeWeapon.CurrentAmmo.ToString();
        string reserveAmmoString = activeWeapon.ReserveAmmo.ToString();

        if (_ammoText != null)
        {
            _ammoText.text = currentAmmoString + SLASH_STRING + reserveAmmoString;
        }

        if (_currentAmmoText != null)
        {
            _currentAmmoText.text = currentAmmoString;
        }

        if (_maxAmmoText != null)
        {
            _maxAmmoText.text = reserveAmmoString;
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
            _reloadingPromptText.text = RELOADING_LABEL;
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
            _ammoText.text = NO_AMMO_STRING + SLASH_STRING + NO_AMMO_STRING;
        }

        if (_currentAmmoText != null)
        {
            _currentAmmoText.text = NO_AMMO_STRING;
        }

        if (_maxAmmoText != null)
        {
            _maxAmmoText.text = NO_AMMO_STRING;
        }
    }
}