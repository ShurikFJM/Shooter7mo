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
    private int _remainingGrenades;
    private float _nextThrowTime;

    private void Awake()
    {
        _playerController = GetComponent<NetworkPlayerController>();
    }

    public override void OnNetworkSpawn()
    {
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

        if (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase != RoundPhase.InProgress)
        {
            return;
        }

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;

        bool throwTriggered = false;

        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            throwTriggered = true;
        }

        if (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame)
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