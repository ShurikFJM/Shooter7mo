using Unity.Netcode;
using UnityEngine;

public class SimpleTestDoor : NetworkBehaviour, IInteractable
{
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private Vector3 openOffset = new Vector3(0, 3f, 0);
    private Vector3 closedPosition;

    private void Awake()
    {
        closedPosition = transform.position;
    }

    public string GetInteractionPrompt()
    {
        return isOpen.Value ? "[E] Cerrar Puerta" : "[E] Abrir Puerta";
    }

    public void Interact(ulong interactorClientId)
    {
        if (!IsServer) return;
        isOpen.Value = !isOpen.Value;
    }

    private void Update()
    {
        Vector3 target = isOpen.Value ? closedPosition + openOffset : closedPosition;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5f);
    }
}