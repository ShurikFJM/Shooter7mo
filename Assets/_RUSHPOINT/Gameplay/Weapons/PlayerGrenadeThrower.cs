using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkPlayerController))]
public class PlayerGrenadeThrower : NetworkBehaviour
{
    [SerializeField] private float _throwForce = 22f;
    [SerializeField] private float _upwardForceRatio = 0.22f;
    [SerializeField] private float _spawnForwardOffset = 0.85f;

    [SerializeField] private GameObject _fragGrenadePrefab;
    [SerializeField] private GameObject _smokeGrenadePrefab;
    [SerializeField] private GameObject _stunGrenadePrefab;

    [SerializeField] private int _maxGrenadesPerRound = 1;

    private NetworkPlayerController _playerController;
    private BombInteractor _bombInteractor;
    private NetworkHealth _networkHealth;
    private int _remainingGrenades;
    private float _nextThrowTime;

    private void Awake()
    {
        _playerController = GetComponent<NetworkPlayerController>();
        _bombInteractor = GetComponent<BombInteractor>();
        if (_bombInteractor == null) _bombInteractor = GetComponentInChildren<BombInteractor>(true);

        _networkHealth = GetComponent<NetworkHealth>();
        if (_networkHealth == null) _networkHealth = GetComponentInChildren<NetworkHealth>(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _remainingGrenades = _maxGrenadesPerRound;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.CurrentPhase.OnValueChanged += HandleRoundPhaseChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.CurrentPhase.OnValueChanged -= HandleRoundPhaseChanged;
        }

        base.OnNetworkDespawn();
    }

    private void HandleRoundPhaseChanged(RoundPhase prev, RoundPhase current)
    {
        if (current == RoundPhase.Warmup || current == RoundPhase.InProgress)
        {
            ResetGrenades();
        }
    }

    public void ResetGrenades()
    {
        _remainingGrenades = _maxGrenadesPerRound;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (SpectatorManager.Instance != null && SpectatorManager.Instance.isSpectating) return;

        if (_networkHealth != null && (!_networkHealth.IsAlive.Value || _networkHealth.CurrentHealth.Value <= 0f)) return;

        if (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase != RoundPhase.InProgress) return;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen) return;

        if (_bombInteractor != null && (_bombInteractor.IsPlanting || _bombInteractor.IsDefusing)) return;

        bool throwTriggered = false;

        bool dedicatedGrenadePressed = false;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
            {
                dedicatedGrenadePressed = true;
            }
        }

        bool dropKeyTriggered = false;
        if (InputManager.Instance != null)
        {
            dropKeyTriggered = InputManager.Instance.WasDropTriggered;
        }
        else
        {
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) dropKeyTriggered = true;
            if (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame) dropKeyTriggered = true;
        }

        if (dropKeyTriggered)
        {
            bool carriesBomb = _bombInteractor != null && _bombInteractor.HasBomb;
            if (!carriesBomb)
            {
                throwTriggered = true;
            }
        }

        if (dedicatedGrenadePressed)
        {
            throwTriggered = true;
        }

        if (throwTriggered && CanThrow())
        {
            ExecuteThrow();
        }
    }

    private bool CanThrow()
    {
        return _remainingGrenades > 0 && Time.time >= _nextThrowTime;
    }

    private void ExecuteThrow()
    {
        _remainingGrenades--;
        _nextThrowTime = Time.time + 1.2f;

        if (_playerController != null)
        {
            //_playerController.TriggerGrenadeTPS();
        }

        Transform camTransform = _playerController.PlayerCamera != null ? _playerController.PlayerCamera.transform : transform;
        Vector3 spawnPosition = camTransform.position + (camTransform.forward * _spawnForwardOffset) + (Vector3.up * 0.1f);
        Vector3 throwDirection = (camTransform.forward + (Vector3.up * _upwardForceRatio)).normalized;
        Vector3 throwVelocity = throwDirection * _throwForce;

        PlayerRoleType currentRole = _playerController.selectedRole.Value;
        RequestSpawnGrenadeServerRpc(spawnPosition, throwVelocity, currentRole);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnGrenadeServerRpc(Vector3 spawnPosition, Vector3 throwVelocity, PlayerRoleType role, RpcParams rpcParams = default)
    {
        GameObject prefabToSpawn = null;

        switch (role)
        {
            case PlayerRoleType.Tank:
                prefabToSpawn = _fragGrenadePrefab;
                break;
            case PlayerRoleType.Assault:
                prefabToSpawn = _smokeGrenadePrefab;
                break;
            case PlayerRoleType.Medic:
                prefabToSpawn = _stunGrenadePrefab;
                break;
            default:
                prefabToSpawn = _fragGrenadePrefab;
                break;
        }

        if (prefabToSpawn == null) return;

        GameObject grenadeInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        NetworkObject netObj = grenadeInstance.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn(true);

            PlayerTeam playerTeam = GetComponent<PlayerTeam>();
            Team team = playerTeam != null ? playerTeam.CurrentTeam.Value : Team.Neutral;

            NetworkGrenadeBase grenadeBase = grenadeInstance.GetComponent<NetworkGrenadeBase>();
            if (grenadeBase != null)
            {
                grenadeBase.InitializeGrenadeServer(rpcParams.Receive.SenderClientId, team);
                grenadeBase.LaunchGrenadeClientRpc(throwVelocity, new NetworkObjectReference(NetworkObject));
            }
        }
    }
}