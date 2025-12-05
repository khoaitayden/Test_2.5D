using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    
    [Header("Timing Settings")]
    [Tooltip("How long the player is frozen in place.")]
    [SerializeField] private float playerFreezeTime = 0.5f;

    [Tooltip("How long the door ignores input (prevents spamming). Should be longer than Freeze Time.")]
    [SerializeField] private float doorCooldownTime = 1.2f;
    
    [Header("Conditions")]
    [SerializeField] private string requiredKeyName = ""; 

    [Header("Feedback")]
    [SerializeField] private string openPrompt = "Open";
    [SerializeField] private string closePrompt = "Close";
    [SerializeField] private string lockedPrompt = "Locked";
    
    private Animator animator;
    private int animBoolID;
    private bool isBusy = false; // Controls the "Spam" lock

    void Awake()
    {
        animator = GetComponent<Animator>();
        animBoolID = Animator.StringToHash("IsOpen");
        animator.SetBool(animBoolID, isOpen);
    }

    public bool Interact(GameObject interactor)
    {
        // 1. REJECT SPAM (Based on doorCooldownTime)
        if (isBusy) return false;

        // 2. CHECK LOCK
        if (isLocked)
        {
            // Add Inventory check logic here
            // return false; 
        }

        // 3. START SEQUENCE
        StartCoroutine(OperationRoutine(interactor));
        return true;
    }

    private IEnumerator OperationRoutine(GameObject interactor)
    {
        isBusy = true; // Lock the door immediately so it can't be clicked again
        TraceEventBus.Emit(transform.position, TraceType.EnviromentNoise);
        // 1. FREEZE PLAYER (Uses the specific freeze duration)
        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.FreezeInteraction(playerFreezeTime);
        }

        // 2. PERFORM TOGGLE
        isOpen = !isOpen;
        animator.SetBool(animBoolID, isOpen);
        
        // Play sound here...
        
        InteractionManager.Instance?.ReportInteraction(this.gameObject, isOpen ? "DoorOpened" : "DoorClosed");

        // 3. WAIT FOR COOLDOWN
        // The player might be able to move already, but the door is still finishing its animation
        yield return new WaitForSeconds(doorCooldownTime);
        
        isBusy = false; // Allow interaction again
    }

    public string GetInteractionPrompt()
    {
        if (isBusy) return ""; // Hide text while animating
        if (isLocked) return lockedPrompt;
        return isOpen ? closePrompt : openPrompt;
    }
}