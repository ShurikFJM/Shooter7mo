using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TacticalHUD : MonoBehaviour
{
    private const string _NO_AMMO_STRING = "-";
    private const string _SLASH_STRING = " / ";
    private const string _RELOADING_LABEL = "RELOADING...";
    private const string _DEFAULT_HALFTIME_MESSAGE = "¡CAMBIO DE BANDO!\n<size=60%>SWITCHING SIDES</size>";
    private const float DEFAULT_BANNER_DURATION = 4.5f;

    [SerializeField] private GameObject _combatHudRoot;

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

    public NetworkHealth PlayerHealth { get; set; }
    public WeaponInventory Inventory { get; set; }

    private bool _hasSubscribedToRoundManager;
    private bool _hasShownHalftimeForCurrentMatch;
    private Coroutine _halftimeBannerCoroutine;

    private void Awake()
    {
        HideReloadingPopup();
        HideHalftimeBanner();
    }

    private void OnDisable()
    {
        UnsubscribeFromRoundManager();
    }

    private void Update()
    {
        EnsureLocalPlayerReferences();
        TrySubscribeToRoundManager();

        bool isSpectatingOrDead = IsLocalPlayerSpectatingOrDead();
        SetCombatHudVisible(!isSpectatingOrDead);

        if (isSpectatingOrDead)
        {
            HideReloadingPopup();
            return;
        }

        UpdateHealthAndArmor();
        UpdateAmmoDisplay();
    }

    private bool IsLocalPlayerSpectatingOrDead()
    {
        if (SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating)
        {
            return true;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                PlayerTeam teamComp = localPlayer.GetComponent<PlayerTeam>();
                if (teamComp != null && teamComp.CurrentTeam.Value == Team.Neutral)
                {
                    return true;
                }
            }
        }

        if (PlayerHealth != null && (!PlayerHealth.IsAlive.Value || PlayerHealth.CurrentHealth.Value <= 0f))
        {
            return true;
        }

        return false;
    }

    private void SetCombatHudVisible(bool visible)
    {
        if (_combatHudRoot != null)
        {
            if (_combatHudRoot.activeSelf != visible)
            {
                _combatHudRoot.SetActive(visible);
            }
            return;
        }

        if (_healthText != null && _healthText.gameObject.activeSelf != visible) _healthText.gameObject.SetActive(visible);
        if (_armorText != null && _armorText.gameObject.activeSelf != visible) _armorText.gameObject.SetActive(visible);
        if (_healthSlider != null && _healthSlider.gameObject.activeSelf != visible) _healthSlider.gameObject.SetActive(visible);
        if (_armorSlider != null && _armorSlider.gameObject.activeSelf != visible) _armorSlider.gameObject.SetActive(visible);
        if (_ammoText != null && _ammoText.gameObject.activeSelf != visible) _ammoText.gameObject.SetActive(visible);
        if (_currentAmmoText != null && _currentAmmoText.gameObject.activeSelf != visible) _currentAmmoText.gameObject.SetActive(visible);
        if (_maxAmmoText != null && _maxAmmoText.gameObject.activeSelf != visible) _maxAmmoText.gameObject.SetActive(visible);
    }

    private void TrySubscribeToRoundManager()
    {
        if (_hasSubscribedToRoundManager || RoundManager.Instance == null) return;

        RoundManager.Instance.CurrentPhase.OnValueChanged += HandleRoundPhaseChanged;
        RoundManager.Instance.RoundNumber.OnValueChanged += HandleRoundNumberChanged;
        _hasSubscribedToRoundManager = true;
    }

    private void UnsubscribeFromRoundManager()
    {
        if (!_hasSubscribedToRoundManager || RoundManager.Instance == null) return;

        RoundManager.Instance.CurrentPhase.OnValueChanged -= HandleRoundPhaseChanged;
        RoundManager.Instance.RoundNumber.OnValueChanged -= HandleRoundNumberChanged;
        _hasSubscribedToRoundManager = false;
    }

    private void HandleRoundNumberChanged(int previousRound, int currentRound)
    {
        if (currentRound <= 1)
        {
            _hasShownHalftimeForCurrentMatch = false;
        }

        CheckAndTriggerHalftimeBanner();
    }

    private void HandleRoundPhaseChanged(RoundPhase previousPhase, RoundPhase currentPhase)
    {
        CheckAndTriggerHalftimeBanner();
    }

    private void CheckAndTriggerHalftimeBanner()
    {
        if (RoundManager.Instance == null) return;

        bool isHalftimeRound = RoundManager.Instance.RoundNumber.Value == RoundManager.Instance.HalftimeRound;
        bool isWarmup = RoundManager.Instance.CurrentPhase.Value == RoundPhase.Warmup;

        if (isHalftimeRound && isWarmup && !_hasShownHalftimeForCurrentMatch)
        {
            _hasShownHalftimeForCurrentMatch = true;
            ShowHalftimeBanner();
        }
    }

    public void ShowHalftimeBanner(string customMessage = null)
    {
        if (_halftimeBannerCoroutine != null)
        {
            StopCoroutine(_halftimeBannerCoroutine);
        }

        _halftimeBannerCoroutine = StartCoroutine(HalftimeBannerRoutine(customMessage));
    }

    private IEnumerator HalftimeBannerRoutine(string customMessage)
    {
        if (_halftimeBannerRoot != null)
        {
            _halftimeBannerRoot.SetActive(true);
        }

        if (_halftimeBannerText != null)
        {
            _halftimeBannerText.text = string.IsNullOrEmpty(customMessage) ? _DEFAULT_HALFTIME_MESSAGE : customMessage;
        }

        yield return new WaitForSeconds(_bannerDisplayDuration);

        HideHalftimeBanner();
        _halftimeBannerCoroutine = null;
    }

    private void HideHalftimeBanner()
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

        if (PlayerHealth == null || PlayerHealth.gameObject != localPlayer.gameObject)
        {
            PlayerHealth = localPlayer.GetComponent<NetworkHealth>();
            if (PlayerHealth == null)
            {
                PlayerHealth = localPlayer.GetComponentInChildren<NetworkHealth>(true);
            }
        }

        if (Inventory == null || Inventory.gameObject != localPlayer.gameObject)
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
            WeaponBase[] childWeapons = Inventory.GetComponentsInChildren<WeaponBase>(true);
            for (int i = 0; i < childWeapons.Length; i++)
            {
                if (childWeapons[i] != null && childWeapons[i].gameObject.activeInHierarchy)
                {
                    weapon = childWeapons[i];
                    break;
                }
            }

            if (weapon == null && childWeapons.Length > 0)
            {
                weapon = childWeapons[0];
            }
        }

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