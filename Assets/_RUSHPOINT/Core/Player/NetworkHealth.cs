using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    private const float _DEFAULT_MAX_HEALTH = 100f;
    private const float _DEFAULT_MAX_ARMOR = 50f;

    [SerializeField]
    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        _DEFAULT_MAX_HEALTH,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField]
    private NetworkVariable<float> _currentArmor = new NetworkVariable<float>(
        _DEFAULT_MAX_ARMOR,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField]
    private NetworkVariable<float> _maxHealth = new NetworkVariable<float>(
        _DEFAULT_MAX_HEALTH,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField]
    private NetworkVariable<float> _maxArmor = new NetworkVariable<float>(
        _DEFAULT_MAX_ARMOR,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField]
    private NetworkVariable<bool> _isAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> CurrentHealth => _currentHealth;
    public NetworkVariable<float> CurrentArmor => _currentArmor;
    public NetworkVariable<float> CurrentShield => _currentArmor;
    public NetworkVariable<float> MaxHealth => _maxHealth;
    public NetworkVariable<float> MaxArmor => _maxArmor;
    public NetworkVariable<bool> IsAlive => _isAlive;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth.Value;
            _currentArmor.Value = _maxArmor.Value;
            _isAlive.Value = true;
        }
    }

    public void SetMaxStatsServer(float maxHealth, float maxArmor)
    {
        if (!IsServer) return;

        _maxHealth.Value = maxHealth;
        _maxArmor.Value = maxArmor;
        _currentHealth.Value = maxHealth;
        _currentArmor.Value = maxArmor;
        _isAlive.Value = true;
    }

    public void ResetHealthServer()
    {
        if (!IsServer) return;

        _currentHealth.Value = _maxHealth.Value;
        _currentArmor.Value = _maxArmor.Value;
        _isAlive.Value = true;

        OnResetHealthClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnResetHealthClientRpc()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(float damage, HitboxType hitboxType, ulong attackerClientId)
    {
        if (!_isAlive.Value) return;

        float remainingDamage = damage;

        if (_currentArmor.Value > 0f)
        {
            if (_currentArmor.Value >= remainingDamage)
            {
                _currentArmor.Value -= remainingDamage;
                remainingDamage = 0f;
            }
            else
            {
                remainingDamage -= _currentArmor.Value;
                _currentArmor.Value = 0f;
            }
        }

        if (remainingDamage > 0f)
        {
            _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - remainingDamage);
        }

        if (_currentHealth.Value <= 0f)
        {
            _isAlive.Value = false;
            OnDeathClientRpc();
        }
    }

    public void TakeDamage(float damage, HitboxType hitboxType, ulong attackerClientId)
    {
        TakeDamageServerRpc(damage, hitboxType, attackerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HealServerRpc(float healAmount)
    {
        if (!_isAlive.Value) return;
        _currentHealth.Value = Mathf.Min(_maxHealth.Value, _currentHealth.Value + healAmount);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnDeathClientRpc()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}