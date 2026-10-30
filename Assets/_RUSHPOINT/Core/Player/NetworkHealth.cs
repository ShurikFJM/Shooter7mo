using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private float _defaultMaxHealth = 100f;
    [SerializeField] private float _defaultMaxArmor = 100f;
    [SerializeField] private bool _enableFriendlyFire = false;

    [SerializeField] private GameObject _characterModelRoot;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> MaxHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> CurrentArmor = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> MaxArmor = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        IsAlive.OnValueChanged += HandleIsAliveChanged;

        if (IsServer)
        {
            ResetHealthServer();
        }

        ApplyLivingState(IsAlive.Value);
    }

    public void SetSpectatorStateServer()
    {
        if (!IsServer) return;
        IsAlive.Value = false;
        CurrentHealth.Value = 0f;
    }

    public override void OnNetworkDespawn()
    {
        IsAlive.OnValueChanged -= HandleIsAliveChanged;
    }

    private void HandleIsAliveChanged(bool previousState, bool isAlive)
    {
        ApplyLivingState(isAlive);
    }

    private void ApplyLivingState(bool isAlive)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = isAlive;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = isAlive;
        }

        if (_characterModelRoot != null)
        {
            _characterModelRoot.SetActive(isAlive);
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                {
                    renderers[i].enabled = isAlive;
                }
            }
        }

        if (IsOwner)
        {
            PlayerInput input = GetComponent<PlayerInput>();
            Camera playerCam = GetComponentInChildren<Camera>(true);

            if (!isAlive)
            {
                if (input != null) input.DeactivateInput();

                PlayerTeam teamComp = GetComponent<PlayerTeam>();
                Team myTeam = teamComp != null ? teamComp.CurrentTeam.Value : Team.Red;

                if (SpectatorManager.Instance != null && playerCam != null)
                {
                    SpectatorManager.Instance.StartSpectating(myTeam, playerCam, transform, 1.6f);
                }
            }
            else
            {
                if (SpectatorManager.Instance != null && SpectatorManager.Instance.isSpectating)
                {
                    SpectatorManager.Instance.StopSpectating();
                }

                if (input != null) input.ActivateInput();
            }
        }
    }

    public void ResetHealthServer()
    {
        if (!IsServer) return;

        MaxHealth.Value = _defaultMaxHealth;
        CurrentHealth.Value = _defaultMaxHealth;
        MaxArmor.Value = _defaultMaxArmor;
        CurrentArmor.Value = _defaultMaxArmor;
        IsAlive.Value = true;
    }

    public void SetMaxStatsServer(float maxHealth, float maxArmor)
    {
        if (!IsServer) return;

        _defaultMaxHealth = maxHealth;
        _defaultMaxArmor = maxArmor;

        MaxHealth.Value = maxHealth;
        MaxArmor.Value = maxArmor;
        CurrentHealth.Value = maxHealth;
        CurrentArmor.Value = maxArmor;
        IsAlive.Value = true;
    }

    [Rpc(SendTo.Server)]
    public void HealServerRpc(float amount, RpcParams rpcParams = default)
    {
        if (!IsServer || !IsAlive.Value) return;
        CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + amount, MaxHealth.Value);
    }

    public void HealServer(float amount)
    {
        if (!IsServer || !IsAlive.Value) return;
        CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + amount, MaxHealth.Value);
    }

    public void ApplyDamageServer(float baseDamage, HitboxType hitboxType, ulong attackerClientId)
    {
        if (!IsServer || !IsAlive.Value) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var attackerClient))
        {
            if (attackerClient.PlayerObject != null)
            {
                PlayerTeam attackerTeamComp = attackerClient.PlayerObject.GetComponent<PlayerTeam>();
                if (attackerTeamComp == null) attackerTeamComp = attackerClient.PlayerObject.GetComponentInChildren<PlayerTeam>();

                PlayerTeam myTeamComp = GetComponent<PlayerTeam>();
                if (myTeamComp == null) myTeamComp = GetComponentInChildren<PlayerTeam>();

                if (attackerTeamComp != null && myTeamComp != null)
                {
                    Team attackerTeam = attackerTeamComp.CurrentTeam.Value;
                    Team victimTeam = myTeamComp.CurrentTeam.Value;

                    if (attackerTeam == victimTeam && attackerTeam != Team.Neutral && !_enableFriendlyFire)
                    {
                        return;
                    }
                }
            }
        }

        float calculatedDamage = baseDamage;
        if (hitboxType == HitboxType.Head)
        {
            calculatedDamage *= 2.5f;
        }
        else if (hitboxType == HitboxType.Legs)
        {
            calculatedDamage *= 0.75f;
        }

        if (CurrentArmor.Value > 0f)
        {
            float armorAbsorption = calculatedDamage * 0.5f;
            float armorToDeduct = Mathf.Min(CurrentArmor.Value, armorAbsorption);

            CurrentArmor.Value -= armorToDeduct;
            calculatedDamage -= armorToDeduct;
        }

        CurrentHealth.Value = Mathf.Max(0f, CurrentHealth.Value - calculatedDamage);

        if (CurrentHealth.Value <= 0f)
        {
            IsAlive.Value = false;
        }
    }
}