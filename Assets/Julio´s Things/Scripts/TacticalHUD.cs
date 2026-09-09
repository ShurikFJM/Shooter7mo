using UnityEngine;
using UnityEngine.UI; 
using TMPro;          

public class TacticalHUD : MonoBehaviour
{
    [Header("Referencias de Datos")]
    public PlayerStats playerStats;
    public WeaponInventory inventory;

    [Header("UI - Vida y Armadura (TextMeshPro)")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI armorText;

    [Header("UI - Munición (TextMeshPro & RawImage)")]
    public TextMeshProUGUI currentAmmoText;
    public TextMeshProUGUI maxAmmoText;
    public RawImage ammoIconImage; 

    [Header("UI - Inventario Estilo CS2 (RawImage)")]
    public RawImage slot1Highlight;
    public RawImage slot2Highlight;
    public RawImage slot3Highlight;

    public Color activeColor = new Color(1f, 1f, 1f, 0.9f);
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    void Update()
    {
        UpdateStatsDisplay();
        UpdateAmmoDisplay();
        UpdateInventoryDisplay();
    }

    void UpdateStatsDisplay()
    {
        if (playerStats != null)
        {
            if (healthText != null) healthText.text = Mathf.CeilToInt(playerStats.health).ToString();
            if (armorText != null) armorText.text = Mathf.CeilToInt(playerStats.armor).ToString();
        }
    }

    void UpdateAmmoDisplay()
    {
        if (inventory != null && inventory.ActiveWeapon != null)
        {
            WeaponBase weapon = inventory.ActiveWeapon;

            if (currentAmmoText != null) currentAmmoText.text = weapon.CurrentAmmo.ToString();
            if (maxAmmoText != null) maxAmmoText.text = weapon.MaxAmmo.ToString();

            if (ammoIconImage != null)
            {
                if (weapon.data != null && weapon.data.ammoIcon != null)
                {
                    ammoIconImage.texture = weapon.data.ammoIcon;
                    ammoIconImage.enabled = true; 
                    ammoIconImage.gameObject.SetActive(true);
                }
                else
                {
                    if (weapon.data == null)
                        Debug.LogWarning($"[HUD] El arma '{weapon.name}' no tiene asignado el ScriptableObject 'Data' en su inspector.");
                    else if (weapon.data.ammoIcon == null)
                        Debug.LogWarning($"[HUD] El asset '{weapon.data.name}' no tiene una textura asignada en 'Ammo Icon'.");

                    ammoIconImage.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (currentAmmoText != null) currentAmmoText.text = "-";
            if (maxAmmoText != null) maxAmmoText.text = "-";
            if (ammoIconImage != null) ammoIconImage.gameObject.SetActive(false);
        }
    }

    void UpdateInventoryDisplay()
    {
        if (inventory == null) return;

        int activeIndex = inventory.ActiveSlotIndex;

        if (slot1Highlight != null) slot1Highlight.color = (activeIndex == 1) ? activeColor : inactiveColor;
        if (slot2Highlight != null) slot2Highlight.color = (activeIndex == 2) ? activeColor : inactiveColor;
        if (slot3Highlight != null) slot3Highlight.color = (activeIndex == 3) ? activeColor : inactiveColor;
    }
}