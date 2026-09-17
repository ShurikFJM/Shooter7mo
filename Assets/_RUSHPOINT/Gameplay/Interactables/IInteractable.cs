public interface IInteractable
{
    string GetInteractionPrompt();
    void Interact(ulong interactorClientId);
}