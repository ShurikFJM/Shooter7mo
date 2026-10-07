using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum BombState : byte
{
    Dropped,
    Carried,
    Planting,
    Planted,
    Defusing,
    Defused,
    Exploded
}

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class Bomb : NetworkBehaviour
{
    private const float _RAYCAST_DOWN_DISTANCE = 3f;
    private const float _GROUND_OFFSET_Y = 0.05f;

    [SerializeField] private float _throwForwardForce = 2.5f;
    [SerializeField] private float _throwUpwardForce = 0.8f;
    [SerializeField] private float _chestHeightOffset = 0.8f;
    [SerializeField] private float _throwTorqueForce = 1.5f;
    [SerializeField] private float _plantDuration = 4f;
    [SerializeField] private float _defuseDuration = 7f;
    [SerializeField] private float _detonationDuration = 45f;
    [SerializeField] private float _dropCooldownDuration = 1.2f;
    [SerializeField] private float _bombMass = 5f;
    [SerializeField] private float _bombDrag = 1.5f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Collider _pickupCollider;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private NetworkTransform _networkTransform;

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

    public NetworkVariable<double> PlantedServerTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private ulong _lastDroppedClientId = ulong.MaxValue;
    private double _lastDroppedServerTime = 0;

    public float PlantDuration => _plantDuration;
    public float DefuseDuration => _defuseDuration;
    public float DetonationTimeDuration => _detonationDuration;
    public Team BombCarrierTeam => Team.Red;
    public Team DefuseTeam => Team.Blue;

    private void Awake()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (_pickupCollider == null)
        {
            _pickupCollider = GetComponent<Collider>();
        }

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_rigidbody != null)
        {
            _rigidbody.mass = _bombMass;
            _rigidbody.linearDamping = _bombDrag;
        }

        if (_networkTransform == null)
        {
            _networkTransform = GetComponent<NetworkTransform>();
        }
    }

    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += HandleStateChanged;
        UpdateVisualsAndPhysics(State.Value);
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(BombState previousState, BombState currentState)
    {
        UpdateVisualsAndPhysics(currentState);
    }

    private void UpdateVisualsAndPhysics(BombState currentState)
    {
        switch (currentState)
        {
            case BombState.Dropped:
                SetVisualsActive(true);
                SetPhysicsActive(true, false);
                break;

            case BombState.Carried:
            case BombState.Planting:
                SetVisualsActive(false);
                SetPhysicsActive(false, false);
                break;

            case BombState.Planted:
            case BombState.Defusing:
                SetVisualsActive(true);
                SetPhysicsActive(false, true);
                break;

            case BombState.Defused:
            case BombState.Exploded:
                SetVisualsActive(false);
                SetPhysicsActive(false, false);
                break;
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = active;
        }
    }

    private void SetPhysicsActive(bool enablePhysics, bool asTriggerOnly)
    {
        if (_pickupCollider != null)
        {
            _pickupCollider.enabled = enablePhysics || asTriggerOnly;
            _pickupCollider.isTrigger = asTriggerOnly || !enablePhysics;
        }

        if (_rigidbody != null)
        {
            if (enablePhysics && !asTriggerOnly)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            else
            {
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (State.Value != BombState.Dropped) return;

        NetworkObject playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null) return;

        if (playerNetObj.OwnerClientId == _lastDroppedClientId)
        {
            double elapsedSinceDrop = NetworkManager.Singleton.ServerTime.Time - _lastDroppedServerTime;
            if (elapsedSinceDrop < _dropCooldownDuration) return;
        }

        PlayerTeam teamComp = playerNetObj.GetComponent<PlayerTeam>();
        if (teamComp == null || teamComp.CurrentTeam.Value != BombCarrierTeam) return;

        NetworkHealth health = playerNetObj.GetComponent<NetworkHealth>();
        if (health != null && (!health.IsAlive.Value || health.CurrentHealth.Value <= 0f)) return;

        PickUpBombServer(playerNetObj.OwnerClientId);
    }

    public void PickUpBombServer(ulong clientId)
    {
        if (!IsServer) return;

        _lastDroppedClientId = ulong.MaxValue;
        CarrierClientId.Value = clientId;
        State.Value = BombState.Carried;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPickupServerRpc(ulong requesterClientId)
    {
        if (!IsServer) return;
        if (State.Value != BombState.Dropped) return;

        if (requesterClientId == _lastDroppedClientId)
        {
            double elapsedSinceDrop = NetworkManager.Singleton.ServerTime.Time - _lastDroppedServerTime;
            if (elapsedSinceDrop < _dropCooldownDuration) return;
        }

        PickUpBombServer(requesterClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDropServerRpc(ulong requesterClientId, Vector3 playerPosition, Vector3 playerForward)
    {
        if (!IsServer) return;
        if (State.Value != BombState.Carried && State.Value != BombState.Planting) return;
        if (CarrierClientId.Value != requesterClientId) return;

        _lastDroppedClientId = requesterClientId;
        _lastDroppedServerTime = NetworkManager.Singleton.ServerTime.Time;

        Vector3 spawnOrigin = playerPosition + Vector3.up * _chestHeightOffset + playerForward * 0.6f;
        Quaternion throwRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        TeleportBomb(spawnOrigin, throwRotation);

        CarrierClientId.Value = ulong.MaxValue;
        State.Value = BombState.Dropped;

        SetPhysicsActive(true, false);
        if (_rigidbody != null)
        {
            Vector3 throwVector = (playerForward * _throwForwardForce) + (Vector3.up * _throwUpwardForce);
            _rigidbody.linearVelocity = throwVector;
            _rigidbody.AddTorque(Random.insideUnitSphere * _throwTorqueForce, ForceMode.Impulse);
        }

        SyncDropVisualsClientRpc(spawnOrigin, throwRotation);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SyncDropVisualsClientRpc(Vector3 origin, Quaternion rotation)
    {
        if (!IsServer)
        {
            TeleportBomb(origin, rotation);
            UpdateVisualsAndPhysics(BombState.Dropped);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPlantServerRpc(ulong requesterClientId, NetworkBehaviourReference siteRef, Vector3 plantPosition, Quaternion plantRotation)
    {
        if (!IsServer) return;
        if (State.Value != BombState.Carried && State.Value != BombState.Planting) return;
        if (CarrierClientId.Value != requesterClientId) return;

        CarrierClientId.Value = ulong.MaxValue;
        PlantedServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        State.Value = BombState.Planted;

        TeleportBomb(plantPosition, plantRotation);
        SetPhysicsActive(false, true);
        SyncPlantTransformClientRpc(plantPosition, plantRotation);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SyncPlantTransformClientRpc(Vector3 plantPosition, Quaternion plantRotation)
    {
        TeleportBomb(plantPosition, plantRotation);
        UpdateVisualsAndPhysics(BombState.Planted);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDefuseServerRpc(ulong requesterClientId)
    {
        if (!IsServer) return;
        if (State.Value != BombState.Planted && State.Value != BombState.Defusing) return;

        State.Value = BombState.Defused;
    }

    public void ServerResetBomb(Vector3 resetPosition)
    {
        if (!IsServer) return;

        _lastDroppedClientId = ulong.MaxValue;
        _lastDroppedServerTime = 0;
        CarrierClientId.Value = ulong.MaxValue;
        PlantedServerTime.Value = 0;
        State.Value = BombState.Dropped;

        SetPhysicsActive(false, false);
        TeleportBomb(resetPosition, Quaternion.identity);

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        SyncResetBombClientRpc(resetPosition);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SyncResetBombClientRpc(Vector3 resetPosition)
    {
        TeleportBomb(resetPosition, Quaternion.identity);
        UpdateVisualsAndPhysics(BombState.Dropped);

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void TeleportBomb(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        if (_networkTransform != null && _networkTransform.IsSpawned)
        {
            _networkTransform.Teleport(position, rotation, transform.localScale);
        }
    }
}