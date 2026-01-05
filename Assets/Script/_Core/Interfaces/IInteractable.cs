using UnityEngine;

public interface IInteractable
{
    // The boolean returns true if interaction was successful, false if conditions failed
    bool Interact(GameObject interactor);
    
    // Returns text to display on UI (e.g., "Press E to Open", "Locked")
    string GetInteractionPrompt();
}