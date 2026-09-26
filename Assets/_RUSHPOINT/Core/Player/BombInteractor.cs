using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // Validación estricta: solo se considera que llevas la bomba si el NetworkVariable del servidor confirma que eres el portador (IA)
    public bool IsCarryingBomb => carriedBomb != null && carriedBomb.State.Value == BombState.Carried && carriedBomb.CarrierClientId.Value == NetworkManager.Singleton.LocalClientId;

    public bool IsPlanting => isPlanting;
    public float PlantProgress01 => plantHoldTime > 0f ? plantProgress / plantHoldTime : 0f;
    public bool HasNearbyBomb => nearbyBomb != null;
    public bool IsInSite => currentSite != null;

    private void Awake()
    {
        if (interactOrigin == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            interactOrigin = cam != null ? cam.transform : transform;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Limpiar referencia de bomba si ya no la poseemos en red
        VerifyCarriedBombState();

        // Bloquear si la pausa o el chat están abiertos
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            if (isPlanting) StopPlant();
            return;
        }

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            if (isPlanting) StopPlant();
            return;
        }

        DetectNearbyBomb();
        DetectCurrentSite();
        HandlePlantHold();
    }

    private void VerifyCarriedBombState()
    {
        if (carriedBomb != null)
        {
            // Si la bomba fue dropeada, plantada o tomada por otro cliente, anulamos la posesión de inmediato
            if (carriedBomb.State.Value != BombState.Carried ||
                carriedBomb.CarrierClientId.Value != NetworkManager.Singleton.LocalClientId)
            {
                carriedBomb = null;
                if (isPlanting)
                {
                    StopPlant();
                }
            }
        }
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
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f, siteLayer);
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

    public void OnInteract(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
        {
            HandleInteractPressed();
        }
        else
        {
            StopPlant();
        }
    }

    private void HandleInteractPressed()
    {
        // 1. Si estás dentro del site y realmente posees la bomba -> Iniciar plantado
        if (IsCarryingBomb && currentSite != null)
        {
            BeginPlant();
        }
        // 2. Si posees la bomba pero estás fuera del site -> Dropear
        else if (IsCarryingBomb)
        {
            RequestDrop();
        }
        // 3. Si no tienes la bomba pero hay una cerca tirada -> Recoger
        else if (nearbyBomb != null)
        {
            RequestPickup(nearbyBomb);
        }
    }

    private void HandlePlantHold()
    {
        if (!isPlanting) return;

        // Cancelar si sales del site o si dejas de tener la bomba autorizada
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
        if (carriedBomb == null || currentSite == null || !IsCarryingBomb) return;

        Vector3 plantPosition = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 2f))
        {
            plantPosition = groundHit.point;
        }

        Quaternion plantRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        var siteRef = new NetworkBehaviourReference(currentSite);

        carriedBomb.RequestPlantServerRpc(
            NetworkManager.Singleton.LocalClientId,
            siteRef,
            plantPosition,
            plantRotation
        );

        carriedBomb = null;
    }

    private void RequestPickup(Bomb bomb)
    {
        bomb.RequestPickupServerRpc(NetworkManager.Singleton.LocalClientId);
        carriedBomb = bomb;
    }

    private void RequestDrop()
    {
        if (carriedBomb == null) return;
        carriedBomb.RequestDropServerRpc(NetworkManager.Singleton.LocalClientId);
        carriedBomb = null;
        StopPlant();
    }
}