using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [Header("Configuración de Salud")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxArmor = 50f;

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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
            CurrentArmor.Value = maxArmor;
        }
    }

    public void TakeDamage(float amount, HitboxType hitboxType, ulong attackerId)
    {
        if (!IsServer || CurrentHealth.Value <= 0f) return;


        if (CurrentArmor.Value > 0f)
        {
            float armorAbsorbed = amount * 0.5f;
            float healthDamage = amount * 0.5f;

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

    [ClientRpc]
    private void DieClientRpc(bool wasHeadshot, ulong killerId)
    {
        if (KillFeedHUD.Instance != null && NetworkManager.Singleton.LocalClientId == killerId)
        {
            KillFeedHUD.Instance.TriggerKillNotification(wasHeadshot);
        }

        gameObject.SetActive(false);
    }
}