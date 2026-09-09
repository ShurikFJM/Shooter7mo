using Unity.Netcode;
using UnityEngine;

public class WeaponInventory : NetworkBehaviour
{
    [Header("Contenedor de Armas")]
    public Transform weaponHolder;

    [Header("Slots de Armas (Componentes WeaponBase en los hijos)")]
    public WeaponBase primaryWeapon;
    public WeaponBase secondaryWeapon;
    public WeaponBase meleeWeapon;

    private int activeSlotIndex = 2;
    private WeaponBase activeWeapon;

    public int ActiveSlotIndex => activeSlotIndex;
    public WeaponBase ActiveWeapon => activeWeapon;

    void Start()
    {
        EquipSlot(activeSlotIndex);
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) CycleSlot(-1);
        else if (scroll < 0f) CycleSlot(1);
    }

    void CycleSlot(int direction)
    {
        int newSlot = activeSlotIndex + direction;
        if (newSlot > 3) newSlot = 1;
        if (newSlot < 1) newSlot = 3;
        if (GetWeaponInSlot(newSlot) != null)
        {
            EquipSlot(newSlot);
        }
    }

    public void EquipSlot(int slotIndex)
    {
        WeaponBase targetWeapon = GetWeaponInSlot(slotIndex);

        if (targetWeapon == null) return;
        if (primaryWeapon != null) primaryWeapon.gameObject.SetActive(false);
        if (secondaryWeapon != null) secondaryWeapon.gameObject.SetActive(false);
        if (meleeWeapon != null) meleeWeapon.gameObject.SetActive(false);

        activeSlotIndex = slotIndex;
        activeWeapon = targetWeapon;
        activeWeapon.gameObject.SetActive(true);
    }

    public WeaponBase GetWeaponInSlot(int index)
    {
        switch (index)
        {
            case 1: return primaryWeapon;
            case 2: return secondaryWeapon;
            case 3: return meleeWeapon;
            default: return null;
        }
    }
}