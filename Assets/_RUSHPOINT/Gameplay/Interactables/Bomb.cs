using Unity.Netcode;
using UnityEngine;

public enum BombState : byte
{
    Dropped,
    Carried,
    Planted,
    Defused,
    Exploded
}

[RequireComponent(typeof(NetworkObject))]
public class Bomb : NetworkBehaviour
{
    private const float _MAX_PERMISSIBLE_DESYNC_DISTANCE = 3f;

    [SerializeField] private float _pickupRadius = 2.5f;
    [SerializeField] private float _plantDuration = 4f;
    [SerializeField] private float _defuseDuration = 5f;
    [SerializeField] private float _detonationTime = 40f;
    [SerializeField] private Rigidbody _bombRigidbody;
    [SerializeField] private Collider _bombCollider;
    [SerializeField] private GameObject _plantedVFX;

    public NetworkVariable<BombState> State = new NetworkVariable<BombState>(
        BombState.Dropped,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> CarrierClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private BombSite _plantedSite;
    private float _detonationTimer;

    public float DetonationTimeDuration => _detonationTime;
    public float PlantDurationTime => _plantDuration;
    public float DefuseDurationTime => _defuseDuration;

    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += HandleStateChanged;
        HandleStateChanged(State.Value, State.Value);
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(BombState previousState, BombState currentState)
    {
        switch (currentState)
        {
            case BombState.Dropped:
                ConfigurePhysicsState(true);
                if (_plantedVFX != null) _plantedVFX.SetActive(false);
                break;

            case BombState.Carried:
                ConfigurePhysicsState(false);
                if (_plantedVFX != null) _plantedVFX.SetActive(false);
                break;

            case BombState.Planted:
                ConfigurePhysicsState(false);
                if (_plantedVFX != null) _plantedVFX.SetActive(true);
                break;

            case BombState.Defused:
            case BombState.Exploded:
                ConfigurePhysicsState(false);
                if (_plantedVFX != null) _plantedVFX.SetActive(false);
                break;
        }
    }

    private void ConfigurePhysicsState(bool isEnabled)
    {
        if (_bombRigidbody != null)
        {
            _bombRigidbody.isKinematic = !isEnabled;
        }

        if (_bombCollider != null)
        {
            _bombCollider.enabled = isEnabled;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (State.Value != BombState.Planted) return;

        _detonationTimer += Time.deltaTime;
        if (_detonationTimer >= _detonationTime)
        {
            ExecuteServerDetonation();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPickupServerRpc(ulong requesterClientId)
    {
        if (State.Value != BombState.Dropped) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient connectedClient)) return;

        NetworkObject playerObject = connectedClient.PlayerObject;
        if (playerObject == null) return;

        float distanceToPlayer = Vector3.Distance(playerObject.transform.position, transform.position);
        if (distanceToPlayer > _pickupRadius) return;

        NetworkObject.TrySetParent(playerObject.transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        CarrierClientId.Value = requesterClientId;
        State.Value = BombState.Carried;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDropServerRpc(ulong requesterClientId)
    {
        if (State.Value != BombState.Carried) return;
        if (CarrierClientId.Value != requesterClientId) return;

        NetworkObject.TryRemoveParent(true);

        State.Value = BombState.Dropped;
        CarrierClientId.Value = ulong.MaxValue;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPlantServerRpc(ulong requesterClientId, NetworkBehaviourReference siteReference, Vector3 requestedPosition, Quaternion requestedRotation)
    {
        if (State.Value != BombState.Carried) return;
        if (CarrierClientId.Value != requesterClientId) return;
        if (!siteReference.TryGet(out BombSite targetSite)) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient connectedClient)) return;

        NetworkObject playerObject = connectedClient.PlayerObject;
        if (playerObject == null) return;
        if (!targetSite.IsPositionInside(playerObject.transform.position)) return;

        NetworkObject.TryRemoveParent(true);

        Vector3 authoritativePlantPosition = requestedPosition;
        if (Vector3.Distance(playerObject.transform.position, requestedPosition) > _MAX_PERMISSIBLE_DESYNC_DISTANCE)
        {
            authoritativePlantPosition = playerObject.transform.position;
        }

        transform.SetPositionAndRotation(authoritativePlantPosition, requestedRotation);

        _plantedSite = targetSite;
        _detonationTimer = 0f;
        State.Value = BombState.Planted;
        CarrierClientId.Value = ulong.MaxValue;

        targetSite.NotifyBombPlantedClientRpc();
    }

    private void ExecuteServerDetonation()
    {
        State.Value = BombState.Exploded;
    }
}