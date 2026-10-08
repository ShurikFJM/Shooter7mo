using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    private const float ARMOR_ABSORPTION_RATIO = 0.5f;
    private const float HEADSHOT_MULTIPLIER = 2.0f;
    private const float LIMB_MULTIPLIER = 0.75f;
    private const float DEFAULT_MAX_HEALTH = 100f;
    private const float DEFAULT_MAX_ARMOR = 50f;

    [SerializeField] private float _baseMaxHealth = DEFAULT_MAX_HEALTH;
    [SerializeField] private float _baseMaxArmor = DEFAULT_MAX_ARMOR;

    public NetworkVariable<float> MaxHealth = new NetworkVariable<float>(
        DEFAULT_MAX_HEALTH,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> MaxArmor = new NetworkVariable<float>(
        DEFAULT_MAX_ARMOR,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        DEFAULT_MAX_HEALTH,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> CurrentArmor = new NetworkVariable<float>(
        DEFAULT_MAX_ARMOR,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool _hasCustomStats;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (!_hasCustomStats)
        {
            MaxHealth.Value = _baseMaxHealth;
            MaxArmor.Value = _baseMaxArmor;
            CurrentHealth.Value = _baseMaxHealth;
            CurrentArmor.Value = _baseMaxArmor;
        }

        IsAlive.Value = CurrentHealth.Value > 0f;
    }

    public void SetMaxStatsServer(float newMaxHealth, float newMaxArmor)
    {
        if (!IsServer) return;

        _hasCustomStats = true;
        MaxHealth.Value = newMaxHealth;
        MaxArmor.Value = newMaxArmor;

        if (IsSpawned)
        {
            CurrentHealth.Value = newMaxHealth;
            CurrentArmor.Value = newMaxArmor;
            IsAlive.Value = true;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HealServerRpc(float healAmount)
    {
        HealDamageServer(healAmount);
    }

    public void Heal(float healAmount)
    {
        if (IsServer)
        {
            HealDamageServer(healAmount);
        }
        else
        {
            HealServerRpc(healAmount);
        }
    }

    public void HealDamageServer(float healAmount)
    {
        if (!IsServer || !IsAlive.Value || healAmount <= 0f) return;

        CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + healAmount, MaxHealth.Value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(float amount, HitboxType hitboxType, ulong attackerId)
    {
        ApplyDamageServer(amount, hitboxType, attackerId);
    }

    public void TakeDamage(float amount, HitboxType hitboxType, ulong attackerId)
    {
        if (IsServer)
        {
            ApplyDamageServer(amount, hitboxType, attackerId);
        }
        else
        {
            TakeDamageServerRpc(amount, hitboxType, attackerId);
        }
    }

    private void ApplyDamageServer(float amount, HitboxType hitboxType, ulong attackerId)
    {
        if (!IsAlive.Value || CurrentHealth.Value <= 0f) return;

        float calculatedDamage = amount;
        if (hitboxType == HitboxType.Head)
        {
            calculatedDamage *= HEADSHOT_MULTIPLIER;
        }
        else if (hitboxType == HitboxType.Legs || hitboxType == HitboxType.Arms)
        {
            calculatedDamage *= LIMB_MULTIPLIER;
        }

        if (CurrentArmor.Value > 0f)
        {
            float armorAbsorbed = calculatedDamage * ARMOR_ABSORPTION_RATIO;
            float healthDamage = calculatedDamage * (1f - ARMOR_ABSORPTION_RATIO);

            if (CurrentArmor.Value >= armorAbsorbed)
            {
                CurrentArmor.Value -= armorAbsorbed;
            }
            else
            {
                healthDamage += armorAbsorbed - CurrentArmor.Value;
                CurrentArmor.Value = 0f;
            }

            CurrentHealth.Value = Mathf.Max(0f, CurrentHealth.Value - healthDamage);
        }
        else
        {
            CurrentHealth.Value = Mathf.Max(0f, CurrentHealth.Value - calculatedDamage);
        }

        if (CurrentHealth.Value <= 0f)
        {
            IsAlive.Value = false;
            DieClientRpc(hitboxType == HitboxType.Head, attackerId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DieClientRpc(bool wasHeadshot, ulong killerId)
    {
        if (KillFeedHUD.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == killerId)
        {
            KillFeedHUD.Instance.TriggerKillNotification(wasHeadshot);
        }
    }

    public void ResetHealthServer()
    {
        if (!IsServer) return;

        CurrentHealth.Value = MaxHealth.Value;
        CurrentArmor.Value = MaxArmor.Value;
        IsAlive.Value = true;
    }

    public void KillServer()
    {
        if (!IsServer) return;

        CurrentHealth.Value = 0f;
        CurrentArmor.Value = 0f;
        IsAlive.Value = false;
    }
}