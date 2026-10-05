using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    private const float _ARMOR_ABSORPTION_RATIO = 0.5f;

    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _maxArmor = 50f;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> CurrentArmor = new NetworkVariable<float>(
        50f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float MaxHealth => _maxHealth;
    public float MaxArmor => _maxArmor;

    private bool _hasCustomStats;

    public void SetMaxStatsServer(float newMaxHealth, float newMaxArmor)
    {
        if (!IsServer) return;

        _maxHealth = newMaxHealth;
        _maxArmor = newMaxArmor;
        _hasCustomStats = true;

        if (IsSpawned)
        {
            CurrentHealth.Value = _maxHealth;
            CurrentArmor.Value = _maxArmor;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !_hasCustomStats)
        {
            CurrentHealth.Value = _maxHealth;
            CurrentArmor.Value = _maxArmor;
        }
    }

    public void ResetHealthServer()
    {
        if (!IsServer) return;

        CurrentHealth.Value = _maxHealth;
        CurrentArmor.Value = _maxArmor;

        ReviveClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReviveClientRpc()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void TakeDamage(float amount, HitboxType hitboxType, ulong attackerId)
    {
        if (!IsServer || CurrentHealth.Value <= 0f) return;

        if (CurrentArmor.Value > 0f)
        {
            float armorAbsorbed = amount * _ARMOR_ABSORPTION_RATIO;
            float healthDamage = amount * (1f - _ARMOR_ABSORPTION_RATIO);

            if (CurrentArmor.Value >= armorAbsorbed)
            {
                CurrentArmor.Value -= armorAbsorbed;
            }
            else
            {
                healthDamage += (armorAbsorbed - CurrentArmor.Value);
                CurrentArmor.Value = 0f;
            }

            CurrentHealth.Value = Mathf.Max(0f, CurrentHealth.Value - healthDamage);
        }
        else
        {
            CurrentHealth.Value = Mathf.Max(0f, CurrentHealth.Value - amount);
        }

        if (CurrentHealth.Value <= 0f)
        {
            DieClientRpc(hitboxType == HitboxType.Head, attackerId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DieClientRpc(bool wasHeadshot, ulong killerId)
    {
        if (KillFeedHUD.Instance != null && NetworkManager.Singleton.LocalClientId == killerId)
        {
            KillFeedHUD.Instance.TriggerKillNotification(wasHeadshot);
        }

        gameObject.SetActive(false);
    }
}