using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : NetworkBehaviour
{
    private const float _SCROLL_DEADZONE = 0.1f;

    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private WeaponBase _primaryWeapon;
    [SerializeField] private WeaponBase _secondaryWeapon;
    [SerializeField] private WeaponBase _meleeWeapon;

    private int _activeSlotIndex = 2;
    private WeaponBase _activeWeapon;

    public int ActiveSlotIndex => _activeSlotIndex;
    public WeaponBase ActiveWeapon => _activeWeapon;

    private void Start()
    {
        EquipSlot(_activeSlotIndex);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            return;
        }

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) EquipSlot(1);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) EquipSlot(2);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) EquipSlot(3);
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) EquipSlot(1);
            if (Gamepad.current.dpad.right.wasPressedThisFrame) EquipSlot(2);
            if (Gamepad.current.dpad.down.wasPressedThisFrame) EquipSlot(3);
            if (Gamepad.current.buttonNorth.wasPressedThisFrame) CycleSlot(1);
        }

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > _SCROLL_DEADZONE) CycleSlot(-1);
            else if (scroll < -_SCROLL_DEADZONE) CycleSlot(1);
        }
    }

    private void CycleSlot(int direction)
    {
        int newSlot = _activeSlotIndex + direction;
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
        if (_primaryWeapon != null) _primaryWeapon.gameObject.SetActive(false);
        if (_secondaryWeapon != null) _secondaryWeapon.gameObject.SetActive(false);
        if (_meleeWeapon != null) _meleeWeapon.gameObject.SetActive(false);

        _activeSlotIndex = slotIndex;
        _activeWeapon = targetWeapon;
        _activeWeapon.gameObject.SetActive(true);
    }

    public WeaponBase GetWeaponInSlot(int index)
    {
        switch (index)
        {
            case 1: return _primaryWeapon;
            case 2: return _secondaryWeapon;
            case 3: return _meleeWeapon;
            default: return null;
        }
    }
}