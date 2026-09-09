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

/// <summary>
/// Objeto de bomba plantable. La autoridad de todo el estado vive en el servidor;
/// los clientes solo leen NetworkVariables y piden acciones vía ServerRpc.
/// Requiere un NetworkObject, un Collider (para detección de pickup) y,
/// opcionalmente, un Rigidbody para que caiga físicamente al ser dropeada.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class Bomb : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float pickupRadius = 2.5f;
    [SerializeField] private float plantDuration = 4f;
    [SerializeField] private float defuseDuration = 5f;
    [SerializeField] private float detonationTime = 40f;

    [Header("Física / Visual")]
    [SerializeField] private Rigidbody bombRigidbody;
    [SerializeField] private Collider bombCollider;
    [SerializeField] private GameObject plantedVFX;

    public NetworkVariable<BombState> State = new NetworkVariable<BombState>(
        BombState.Dropped,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> CarrierClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private BombSite plantedSite;
    private float detonationTimer;

    public float DetonationTime => detonationTime;
    public float PlantDuration => plantDuration;
    public float DefuseDuration => defuseDuration;

    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += HandleStateChanged;
        HandleStateChanged(State.Value, State.Value);
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(BombState previous, BombState current)
    {
        switch (current)
        {
            case BombState.Dropped:
                SetPhysicsEnabled(true);
                if (plantedVFX != null) plantedVFX.SetActive(false);
                break;

            case BombState.Carried:
                SetPhysicsEnabled(false);
                if (plantedVFX != null) plantedVFX.SetActive(false);
                break;

            case BombState.Planted:
                SetPhysicsEnabled(false);
                if (plantedVFX != null) plantedVFX.SetActive(true);
                break;

            case BombState.Defused:
            case BombState.Exploded:
                SetPhysicsEnabled(false);
                if (plantedVFX != null) plantedVFX.SetActive(false);
                break;
        }
    }

    private void SetPhysicsEnabled(bool physicsEnabled)
    {
        if (bombRigidbody != null)
            bombRigidbody.isKinematic = !physicsEnabled;

        if (bombCollider != null)
            bombCollider.enabled = physicsEnabled;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (State.Value != BombState.Planted) return;

        detonationTimer += Time.deltaTime;
        if (detonationTimer >= detonationTime)
        {
            DetonateServer();
        }
    }

    // ---------------- RECOGER ----------------
    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(ulong requesterClientId)
    {
        if (State.Value != BombState.Dropped) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out var client)) return;

        NetworkObject requesterObject = client.PlayerObject;
        if (requesterObject == null) return;

        float distance = Vector3.Distance(requesterObject.transform.position, transform.position);
        if (distance > pickupRadius) return;

        NetworkObject.TrySetParent(requesterObject.transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        CarrierClientId.Value = requesterClientId;
        State.Value = BombState.Carried;
    }

    // ---------------- DROPEAR ----------------
    [ServerRpc(RequireOwnership = false)]
    public void RequestDropServerRpc(ulong requesterClientId)
    {
        if (State.Value != BombState.Carried) return;
        if (CarrierClientId.Value != requesterClientId) return;

        NetworkObject.TryRemoveParent(true);

        State.Value = BombState.Dropped;
        CarrierClientId.Value = ulong.MaxValue;
    }

    // ---------------- PLANTAR ----------------
    [ServerRpc(RequireOwnership = false)]
    public void RequestPlantServerRpc(ulong requesterClientId, NetworkBehaviourReference siteReference)
    {
        if (State.Value != BombState.Carried) return;
        if (CarrierClientId.Value != requesterClientId) return;
        if (!siteReference.TryGet(out BombSite site)) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out var client)) return;

        NetworkObject requesterObject = client.PlayerObject;
        if (requesterObject == null) return;
        if (!site.IsPositionInside(requesterObject.transform.position)) return;

        NetworkObject.TryRemoveParent(true);
        transform.SetPositionAndRotation(site.PlantPoint.position, site.PlantPoint.rotation);

        plantedSite = site;
        detonationTimer = 0f;
        State.Value = BombState.Planted;
        CarrierClientId.Value = ulong.MaxValue;

        site.NotifyBombPlantedClientRpc();
    }

    private void DetonateServer()
    {
        State.Value = BombState.Exploded;
        // TODO: enganchar aquí el evento de fin de ronda (gana el equipo atacante).
    }
}
