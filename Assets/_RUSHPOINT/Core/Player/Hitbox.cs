using UnityEngine;

public enum HitboxType : byte
{
    Head = 0,
    Chest = 1,
    Legs = 2
}

public class Hitbox : MonoBehaviour
{
    [SerializeField] private HitboxType _type = HitboxType.Chest;

    public HitboxType Type
    {
        get => _type;
        set => _type = value;
    }

    public HitboxType hitboxType => _type;
}