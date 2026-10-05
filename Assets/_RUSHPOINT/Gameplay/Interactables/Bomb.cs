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
public class Bomb : NetworkBehaviour, IInteractable
{
    private const float _MAX_PERMISSIBLE_DESYNC_DISTANCE = 4f;

    [SerializeField] private float _pickupRadius = 2.5f;
    [SerializeField] private float _plantDuration = 4f;
    [SerializeField] private float _defuseDuration = 5f;
    [SerializeField] private float _detonationTime = 40f;
    [SerializeField] private Rigidbody _bombRigidbody;
    [SerializeField] private Collider _bombCollider;
    [SerializeField] private GameObject _plantedVFX;
    [SerializeField] private Renderer[] _bombRenderers;

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

    private void Awake()
    {
        if (_bombCollider == null)
        {
            _bombCollider = GetComponent<Collider>();
        }

        if (_bombRigidbody == null)
        {
            _bombRigidbody = GetComponent<Rigidbody>();
        }

        if (_bombRenderers == null || _bombRenderers.Length == 0)
        {
            _bombRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

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
                SetRenderersVisibility(true);
                if (_plantedVFX != null) _plantedVFX.SetActive(false);
                break;

            case BombState.Carried:
                ConfigurePhysicsState(false);
                SetRenderersVisibility(false);
                if (_plantedVFX != null) _plantedVFX.SetActive(false);
                break;

            case BombState.Planted:
                ConfigurePhysicsState(false);
                SetRenderersVisibility(true);
                if (_plantedVFX != null) _plantedVFX.SetActive(true);
                break;

            case BombState.Defused:
            case BombState.Exploded:
                ConfigurePhysicsState(false);
                SetRenderersVisibility(false);
                if (_plantedVFX != null) _plantedVFX.SetActive(false);
                break;
        }
    }

    private void SetRenderersVisibility(bool isVisible)
    {
        if (_bombRenderers == null) return;

        for (int i = 0; i < _bombRenderers.Length; i++)
        {
            if (_bombRenderers[i] != null)
            {
                _bombRenderers[i].enabled = isVisible;
            }
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

    public string GetInteractionPrompt()
    {
        switch (State.Value)
        {
            case BombState.Dropped:
                return "[E] Recoger C4";
            case BombState.Planted:
                return "Mantén [E] para desactivar C4";
            default:
                return string.Empty;
        }
    }

    public void Interact(ulong interactorClientId)
    {
        if (!IsServer) return;

        if (State.Value == BombState.Dropped)
        {
            PickUpDirectServer(interactorClientId);
        }
    }

    private void PickUpDirectServer(ulong requesterClientId)
    {
        if (State.Value != BombState.Dropped) return;
        CarrierClientId.Value = requesterClientId;
        State.Value = BombState.Carried;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPickupServerRpc(ulong requesterClientId)
    {
        if (State.Value != BombState.Dropped) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient connectedClient)) return;

        NetworkObject playerObject = connectedClient.PlayerObject;
        if (playerObject == null) return;

        float distanceToPlayer = Vector3.Distance(playerObject.transform.position, transform.position);
        if (distanceToPlayer > _pickupRadius + 1f) return;

        CarrierClientId.Value = requesterClientId;
        State.Value = BombState.Carried;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDropServerRpc(ulong requesterClientId, Vector3 dropPosition)
    {
        if (State.Value != BombState.Carried) return;
        if (CarrierClientId.Value != requesterClientId) return;

        transform.position = dropPosition;
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDefuseServerRpc(ulong requesterClientId)
    {
        if (State.Value != BombState.Planted) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient connectedClient)) return;

        NetworkObject playerObject = connectedClient.PlayerObject;
        if (playerObject == null) return;

        float distanceToBomb = Vector3.Distance(playerObject.transform.position, transform.position);
        if (distanceToBomb > _pickupRadius + 1f) return;

        State.Value = BombState.Defused;
    }

    private void ExecuteServerDetonation()
    {
        State.Value = BombState.Exploded;
    }
}