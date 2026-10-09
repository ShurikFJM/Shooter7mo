using Unity.Netcode;
using UnityEngine;

public enum HitboxType
{
    Head,
    Chest,
    Legs
}

public class Hitbox : MonoBehaviour
{
    [SerializeField] private NetworkHealth _targetHealth;
    [SerializeField] private HitboxType _hitboxType = HitboxType.Chest;
    [SerializeField] private float _damageMultiplier = 1.0f;

    private void Awake()
    {
        EnsureHealthReference();
    }

    private void EnsureHealthReference()
    {
        if (_targetHealth == null)
        {
            _targetHealth = GetComponentInParent<NetworkHealth>();
            if (_targetHealth == null)
            {
                _targetHealth = transform.root.GetComponent<NetworkHealth>();
            }
        }
    }

    public void ReceiveHit(float baseDamage, ulong attackerId)
    {
        EnsureHealthReference();
        if (_targetHealth == null) return;

        NetworkObject myNetObj = _targetHealth.GetComponent<NetworkObject>();


        if (myNetObj != null && myNetObj.IsSpawned && myNetObj.IsOwner)
        {
            return;
        }

        float calculatedDamage = baseDamage * _damageMultiplier;
        _targetHealth.TakeDamageServerRpc(calculatedDamage, _hitboxType, attackerId);
    }
}