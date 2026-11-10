using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHealth : NetworkBehaviour
{
    private const float CRITICAL_HEAD_DAMAGE_MULTIPLIER = 2.5f;
    private const float LEG_DAMAGE_MULTIPLIER = 0.75f;
    private const float ARMOR_ABSORPTION_RATE = 0.5f;
    private const float SPECTATOR_CAMERA_VERTICAL_OFFSET = 1.6f;

    [SerializeField] private float _defaultMaxHealth = 100f;
    [SerializeField] private float _defaultMaxArmor = 100f;
    [SerializeField] private bool _enableFriendlyFire = false;
    [SerializeField] private GameObject _characterModelRoot;

    private readonly NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<float> _maxHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<float> _currentArmor = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<float> _maxArmor = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<bool> _isAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> CurrentHealth => _currentHealth;
    public NetworkVariable<float> MaxHealth => _maxHealth;
    public NetworkVariable<float> CurrentArmor => _currentArmor;
    public NetworkVariable<float> MaxArmor => _maxArmor;
    public NetworkVariable<bool> IsAlive => _isAlive;

    public override void OnNetworkSpawn()
    {
        _isAlive.OnValueChanged += HandleLivingStateChanged;

        if (IsServer)
        {
            ResetHealthServer();
        }

        ApplyLivingState(_isAlive.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isAlive.OnValueChanged -= HandleLivingStateChanged;
    }

    private void HandleLivingStateChanged(bool previousState, bool isAlive)
    {
        ApplyLivingState(isAlive);
    }

    private void ApplyLivingState(bool isAlive)
    {
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = isAlive;
        }

        RagdollController ragdoll = GetComponentInChildren<RagdollController>();

        if (isAlive)
        {
            if (ragdoll != null)
            {
                ragdoll.ResetRagdoll();
            }

            if (_characterModelRoot != null)
            {
                _characterModelRoot.SetActive(true);
            }
        }
        else
        {
            if (ragdoll != null)
            {
                ragdoll.SetRagdollActive(true);
            }
        }

        if (IsOwner)
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            Camera playerCamera = GetComponentInChildren<Camera>(true);

            if (!isAlive)
            {
                if (playerInput != null)
                {
                    playerInput.DeactivateInput();
                }

                PlayerTeam playerTeamComponent = GetComponent<PlayerTeam>();
                if (playerTeamComponent == null)
                {
                    playerTeamComponent = GetComponentInChildren<PlayerTeam>();
                }

                Team assignedTeam = playerTeamComponent != null ? playerTeamComponent.CurrentTeam.Value : Team.Red;

                if (assignedTeam == Team.Neutral && CustomLobbyManager.SavedTeams != null)
                {
                    if (CustomLobbyManager.SavedTeams.TryGetValue(OwnerClientId, out Team savedLobbyTeam))
                    {
                        assignedTeam = savedLobbyTeam;
                    }
                }

                if (SpectatorManager.Instance != null && playerCamera != null)
                {
                    SpectatorManager.Instance.StartSpectating(assignedTeam, playerCamera, transform, SPECTATOR_CAMERA_VERTICAL_OFFSET);
                }
            }
            else
            {
                if (SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating)
                {
                    SpectatorManager.Instance.StopSpectating();
                }

                if (playerInput != null)
                {
                    playerInput.ActivateInput();
                }
            }
        }
    }

    public void ResetHealthServer()
    {
        if (!IsServer) return;

        _maxHealth.Value = _defaultMaxHealth;
        _currentHealth.Value = _defaultMaxHealth;
        _maxArmor.Value = _defaultMaxArmor;
        _currentArmor.Value = _defaultMaxArmor;
        _isAlive.Value = true;
    }

    public void SetMaxStatsServer(float maxHealth, float maxArmor)
    {
        if (!IsServer) return;

        _defaultMaxHealth = maxHealth;
        _defaultMaxArmor = maxArmor;

        _maxHealth.Value = maxHealth;
        _maxArmor.Value = maxArmor;
        _currentHealth.Value = maxHealth;
        _currentArmor.Value = maxArmor;
        _isAlive.Value = true;
    }

    public void SetSpectatorStateServer()
    {
        if (!IsServer) return;

        _isAlive.Value = false;
        _currentHealth.Value = 0f;
    }

    [Rpc(SendTo.Server)]
    public void HealServerRpc(float amount, RpcParams rpcParams = default)
    {
        if (!IsServer || !_isAlive.Value) return;

        _currentHealth.Value = Mathf.Min(_currentHealth.Value + amount, _maxHealth.Value);
    }

    public void HealServer(float amount)
    {
        if (!IsServer || !_isAlive.Value) return;

        _currentHealth.Value = Mathf.Min(_currentHealth.Value + amount, _maxHealth.Value);
    }

    public void ApplyDamageServer(float baseDamage, HitboxType hitboxType, ulong attackerClientId)
    {
        if (!IsServer || !_isAlive.Value) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out NetworkClient attackerClient))
        {
            if (attackerClient.PlayerObject != null)
            {
                PlayerTeam attackerTeamComponent = attackerClient.PlayerObject.GetComponent<PlayerTeam>();
                if (attackerTeamComponent == null)
                {
                    attackerTeamComponent = attackerClient.PlayerObject.GetComponentInChildren<PlayerTeam>();
                }

                PlayerTeam victimTeamComponent = GetComponent<PlayerTeam>();
                if (victimTeamComponent == null)
                {
                    victimTeamComponent = GetComponentInChildren<PlayerTeam>();
                }

                if (attackerTeamComponent != null && victimTeamComponent != null)
                {
                    Team attackerTeam = attackerTeamComponent.CurrentTeam.Value;
                    Team victimTeam = victimTeamComponent.CurrentTeam.Value;

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
            calculatedDamage *= CRITICAL_HEAD_DAMAGE_MULTIPLIER;
        }
        else if (hitboxType == HitboxType.Legs)
        {
            calculatedDamage *= LEG_DAMAGE_MULTIPLIER;
        }

        if (_currentArmor.Value > 0f)
        {
            float armorAbsorption = calculatedDamage * ARMOR_ABSORPTION_RATE;
            float armorToDeduct = Mathf.Min(_currentArmor.Value, armorAbsorption);

            _currentArmor.Value -= armorToDeduct;
            calculatedDamage -= armorToDeduct;
        }

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - calculatedDamage);

        if (_currentHealth.Value <= 0f)
        {
            _isAlive.Value = false;
            string killerName = "Desconocido";

            Vector3 hitDirection = -transform.forward;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out NetworkClient killerClient))
            {
                if (killerClient.PlayerObject != null)
                {
                    hitDirection = (transform.position - killerClient.PlayerObject.transform.position).normalized;
                }
            }

            TriggerRagdollDeathClientRpc(hitDirection);

            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.RegisterKillAndDeathServer(attackerClientId, OwnerClientId);

                foreach (var stat in ScoreboardManager.Instance.PlayerStats)
                {
                    if (stat.ClientId == attackerClientId)
                    {
                        killerName = stat.PlayerName.ToString();
                        break;
                    }
                }
            }

            if (KillFeedManager.Instance != null)
            {
                KillFeedManager.Instance.BroadcastKillServer(attackerClientId, OwnerClientId);
            }

            PlayerFeedbackUI feedbackUI = GetComponent<PlayerFeedbackUI>();
            if (feedbackUI != null)
            {
                feedbackUI.ShowDeathScreenClientRpc(new FixedString32Bytes(killerName));
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerRagdollDeathClientRpc(Vector3 impactDirection)
    {
        RagdollController ragdoll = GetComponentInChildren<RagdollController>();
        if (ragdoll != null)
        {
            ragdoll.TriggerRagdoll(impactDirection + Vector3.up * 0.25f, 220f);
        }
    }
}