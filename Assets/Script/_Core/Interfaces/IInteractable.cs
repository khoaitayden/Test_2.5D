using UnityEngine;

public interface IInteractable
{
    bool Interact(GameObject interactor);
    string GetInteractionPrompt();
}