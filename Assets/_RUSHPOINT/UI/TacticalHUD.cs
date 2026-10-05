using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TacticalHUD : MonoBehaviour
{
    [SerializeField] private NetworkHealth _playerHealth;
    [SerializeField] private WeaponInventory _inventory;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _armorText;
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _maxAmmoText;
    [SerializeField] private RawImage _ammoIconImage;
    [SerializeField] private RawImage _slot1Highlight;
    [SerializeField] private RawImage _slot2Highlight;
    [SerializeField] private RawImage _slot3Highlight;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _tScoreText;
    [SerializeField] private TextMeshProUGUI _ctScoreText;
    [SerializeField] private Image _timerBackgroundBox;
    [SerializeField] private Color _normalTimerBgColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    [SerializeField] private Color _bombPlantedBgColor = new Color(0.85f, 0.15f, 0.15f, 0.95f);
    [SerializeField] private Color _freezeTimeBgColor = new Color(0.15f, 0.5f, 0.85f, 0.9f);
    [SerializeField] private Color _activeSlotColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color _inactiveSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    public NetworkHealth PlayerHealth
    {
        get => _playerHealth;
        set => _playerHealth = value;
    }

    public WeaponInventory Inventory
    {
        get => _inventory;
        set => _inventory = value;
    }

    private void Update()
    {
        UpdateStatsDisplay();
        UpdateAmmoDisplay();
        UpdateInventoryDisplay();
        UpdateRoundDisplay();
    }

    private void UpdateStatsDisplay()
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
    }

    private void UpdateAmmoDisplay()
    {
        if (_inventory != null && _inventory.ActiveWeapon != null)
        {
            WeaponBase activeWeapon = _inventory.ActiveWeapon;

            if (_currentAmmoText != null)
            {
                _currentAmmoText.text = activeWeapon.CurrentAmmo.ToString();
            }

            if (_maxAmmoText != null)
            {
                _maxAmmoText.text = activeWeapon.MaxAmmo.ToString();
            }

            if (_ammoIconImage != null)
            {
                if (activeWeapon.data != null && activeWeapon.data.ammoIcon != null)
                {
                    _ammoIconImage.texture = activeWeapon.data.ammoIcon;
                    _ammoIconImage.enabled = true;
                    _ammoIconImage.gameObject.SetActive(true);
                }
                else
                {
                    _ammoIconImage.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (_currentAmmoText != null) _currentAmmoText.text = "-";
            if (_maxAmmoText != null) _maxAmmoText.text = "-";
            if (_ammoIconImage != null) _ammoIconImage.gameObject.SetActive(false);
        }
    }

    private void UpdateInventoryDisplay()
    {
        if (_inventory == null) return;

        int activeIndex = _inventory.ActiveSlotIndex;

        if (_slot1Highlight != null)
        {
            _slot1Highlight.color = (activeIndex == 1) ? _activeSlotColor : _inactiveSlotColor;
        }

        if (_slot2Highlight != null)
        {
            _slot2Highlight.color = (activeIndex == 2) ? _activeSlotColor : _inactiveSlotColor;
        }

        if (_slot3Highlight != null)
        {
            _slot3Highlight.color = (activeIndex == 3) ? _activeSlotColor : _inactiveSlotColor;
        }
    }

    private void UpdateRoundDisplay()
    {
        if (RoundManager.Instance == null) return;

        if (_tScoreText != null)
        {
            _tScoreText.text = RoundManager.Instance.TerroristScore.Value.ToString();
        }

        if (_ctScoreText != null)
        {
            _ctScoreText.text = RoundManager.Instance.CounterTerroristScore.Value.ToString();
        }

        float timeRemaining = Mathf.Max(0f, RoundManager.Instance.PhaseTimer.Value);
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        RoundPhase currentPhase = RoundManager.Instance.CurrentPhase.Value;

        if (_timerText != null)
        {
            if (currentPhase == RoundPhase.WaitingForPlayers)
            {
                _timerText.text = "--:--";
            }
            else
            {
                _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        if (_timerBackgroundBox != null)
        {
            if (currentPhase == RoundPhase.FreezeTime)
            {
                _timerBackgroundBox.color = _freezeTimeBgColor;
            }
            else if (currentPhase == RoundPhase.Active)
            {
                _timerBackgroundBox.color = (timeRemaining <= 40f && timeRemaining > 0f && minutes == 0)
                    ? _bombPlantedBgColor
                    : _normalTimerBgColor;
            }
            else
            {
                _timerBackgroundBox.color = _normalTimerBgColor;
            }
        }
    }
}