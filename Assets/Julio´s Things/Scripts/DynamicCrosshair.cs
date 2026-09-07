using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Reticulacao")]
    public RectTransform topTick;
    public RectTransform bottomTick;
    public RectTransform leftTick;
    public RectTransform rightTick;

    [Header("Sensibilidad y Calibracao")]
    public float baseGap = 12f;         
    public float gapScaleMultiplier = 25f; 
    public float maxGap = 160f;         
    public float expandSpeed = 35f;      
    public float contractSpeed = 20f;    

    [Header("Referencao")]
    public WeaponInventory inventory;

    private float currentGap;

    void Start()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<WeaponInventory>();
        }

        currentGap = baseGap;
    }

    void Update()
    {
        if (inventory == null || inventory.ActiveWeapon == null) return;

        WeaponBase activeWeapon = inventory.ActiveWeapon;

        float realTimeSpread = activeWeapon.GetCurrentSpread();
        float targetGap = baseGap + (realTimeSpread * gapScaleMultiplier);

        if (activeWeapon.IsReloading)
        {
            targetGap = baseGap;
        }

        targetGap = Mathf.Clamp(targetGap, baseGap, maxGap);

        float lerpSpeed = (targetGap > currentGap) ? expandSpeed : contractSpeed;
        currentGap = Mathf.Lerp(currentGap, targetGap, Time.deltaTime * lerpSpeed);

        if (topTick != null) topTick.anchoredPosition = new Vector2(0f, currentGap);
        if (bottomTick != null) bottomTick.anchoredPosition = new Vector2(0f, -currentGap);
        if (leftTick != null) leftTick.anchoredPosition = new Vector2(-currentGap, 0f);
        if (rightTick != null) rightTick.anchoredPosition = new Vector2(currentGap, 0f);
    }
}