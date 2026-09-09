using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Agregar junto a NetworkPlayerController. Detecta bombas cercanas y sitios
/// de plantado, y traduce el input de "Interactuar" en las acciones de
/// recoger / dropear / plantar sobre Bomb.cs.
///
/// Requiere en el Input Actions Asset:
///  - Acción "Interact" (botón, ej. tecla E) -> OnInteract
/// Y en el proyecto:
///  - Los jugadores deben tener el tag "Player"
///  - Definir Layers para la bomba y para los BombSite, y asignarlas en el inspector
/// </summary>
public class BombInteractor : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform interactOrigin;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask bombLayer;
    [SerializeField] private LayerMask siteLayer;

    [Header("Plantado")]
    [SerializeField] private float plantHoldTime = 4f;

    private Bomb carriedBomb;
    private Bomb nearbyBomb;
    private BombSite currentSite;
    private float plantProgress;
    private bool isPlanting;

    public bool IsCarryingBomb => carriedBomb != null;
    public bool IsPlanting => isPlanting;
    public float PlantProgress01 => plantHoldTime > 0f ? plantProgress / plantHoldTime : 0f;

    private void Update()
    {
        if (!IsOwner) return;

        DetectNearbyBomb();
        DetectCurrentSite();
        HandlePlantHold();
    }

    private void DetectNearbyBomb()
    {
        if (IsCarryingBomb)
        {
            nearbyBomb = null;
            return;
        }

        Collider[] hits = Physics.OverlapSphere(interactOrigin.position, interactRange, bombLayer);
        nearbyBomb = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            Bomb bomb = hit.GetComponentInParent<Bomb>();
            if (bomb == null || bomb.State.Value != BombState.Dropped) continue;

            float dist = Vector3.Distance(interactOrigin.position, bomb.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearbyBomb = bomb;
            }
        }
    }

    private void DetectCurrentSite()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.15f, siteLayer);
        currentSite = null;

        foreach (Collider hit in hits)
        {
            BombSite site = hit.GetComponent<BombSite>();
            if (site != null)
            {
                currentSite = site;
                break;
            }
        }
    }

    // Enlazar en el Player Input component (Behavior: Send Messages).
    // La acción "Interact" debe usar la interacción "Press" con
    // Trigger Behavior = "Press and Release", así este mismo método
    // se llama tanto al presionar (isPressed = true) como al soltar
    // (isPressed = false) el botón.
    public void OnInteract(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
        {
            HandleInteractPressed();
        }
        else
        {
            // Soltaste el botón: si estabas plantando, se cancela.
            StopPlant();
        }
    }

    private void HandleInteractPressed()
    {
        if (IsCarryingBomb && currentSite != null)
        {
            BeginPlant();
        }
        else if (IsCarryingBomb)
        {
            RequestDrop();
        }
        else if (nearbyBomb != null)
        {
            RequestPickup(nearbyBomb);
        }
    }

    private void HandlePlantHold()
    {
        if (!isPlanting) return;

        // Si te sales del sitio o sueltas la bomba (por otra vía) a mitad de plantado, cancelar
        if (currentSite == null || !IsCarryingBomb)
        {
            StopPlant();
            return;
        }

        plantProgress += Time.deltaTime;
        if (plantProgress >= plantHoldTime)
        {
            FinishPlant();
            StopPlant();
        }
    }

    private void BeginPlant()
    {
        isPlanting = true;
        plantProgress = 0f;
    }

    private void StopPlant()
    {
        isPlanting = false;
        plantProgress = 0f;
    }

    private void FinishPlant()
    {
        if (carriedBomb == null || currentSite == null) return;

        var siteRef = new NetworkBehaviourReference(currentSite);
        carriedBomb.RequestPlantServerRpc(NetworkManager.Singleton.LocalClientId, siteRef);
        carriedBomb = null;
    }

    private void RequestPickup(Bomb bomb)
    {
        bomb.RequestPickupServerRpc(NetworkManager.Singleton.LocalClientId);
        carriedBomb = bomb; // asignación optimista local; el estado real lo confirma el NetworkVariable
    }

    private void RequestDrop()
    {
        if (carriedBomb == null) return;
        carriedBomb.RequestDropServerRpc(NetworkManager.Singleton.LocalClientId);
        carriedBomb = null;
    }
}
