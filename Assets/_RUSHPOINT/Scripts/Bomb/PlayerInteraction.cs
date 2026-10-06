using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float _interactRange = 3f;
    [SerializeField] private LayerMask _interactableLayer = ~0;
    [SerializeField] private Camera _playerCamera;

    private IInteractable _currentInteractable;

    private void Awake()
    {
        if (_playerCamera == null)
        {
            _playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        CheckForInteractable();

        if (Input.GetKeyDown(KeyCode.E) && _currentInteractable != null)
        {
            PerformInteractionServerRpc();
        }
    }

    private void CheckForInteractable()
    {
        if (_playerCamera == null) return;

        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, _interactRange, _interactableLayer))
        {
            _currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }
        else
        {
            _currentInteractable = null;
        }
    }

    [Rpc(SendTo.Server)]
    private void PerformInteractionServerRpc(RpcParams rpcParams = default)
    {
        ulong callerId = rpcParams.Receive.SenderClientId;

        if (_playerCamera == null) return;

        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, _interactRange + 0.5f, _interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(callerId);
            }
        }
    }

    public string GetCurrentPrompt()
    {
        return _currentInteractable != null ? _currentInteractable.GetInteractionPrompt() : string.Empty;
    }
}