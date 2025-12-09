using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Chest Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    
    [Header("Timing Settings")]
    [SerializeField] private float playerFreezeTime = 1.0f;
    [SerializeField] private float chestCooldownTime = 1.5f;

    [Tooltip("Delay before item starts rising (so lid opens first).")]
    [SerializeField] private float itemRiseDelay = 0.5f; 
    
    [Header("Item Container")]
    // CHANGED: Reference the Script, not GameObject
    [SerializeField] private ChestItemDisplay itemDisplayScript; 

    [Header("Feedback")]
    [SerializeField] private string openPrompt = "Open Chest";
    [SerializeField] private string closePrompt = "Close Chest";
    [SerializeField] private string lockedPrompt = "Locked";
    
    private Animator animator;
    private int animBoolID;
    private bool isBusy = false; 

    void Awake()
    {
        animator = GetComponent<Animator>();
        animBoolID = Animator.StringToHash("IsOpen"); 
        animator.SetBool(animBoolID, isOpen);
        
        // Initialize Item State
        if (itemDisplayScript != null)
            itemDisplayScript.SetItemState(isOpen);
    }

    public bool Interact(GameObject interactor)
    {
        if (isBusy || isLocked) return false;
        StartCoroutine(OperationRoutine(interactor));
        return true;
    }

    private IEnumerator OperationRoutine(GameObject interactor)
    {
        isBusy = true; 
        TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseMedium);

        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null) pc.FreezeInteraction(playerFreezeTime);

        // Toggle State
        isOpen = !isOpen;

        if (isOpen)
        {
            // --- OPENING ---
            animator.SetBool(animBoolID, true); // Start Lid Opening
            
            // Wait for lid to clear the item
            yield return new WaitForSeconds(itemRiseDelay);
            
            // Tell Item to Rise
            if(itemDisplayScript != null) itemDisplayScript.SetItemState(true);
        }
        else
        {
            // --- CLOSING ---
            // Tell Item to Sink FIRST
            if(itemDisplayScript != null) itemDisplayScript.SetItemState(false);
            
            // Wait for item to sink (approx same as rise delay or shorter)
            yield return new WaitForSeconds(0.3f); 
            
            animator.SetBool(animBoolID, false); // Start Lid Closing
        }

        InteractionManager.Instance?.ReportInteraction(this.gameObject, isOpen ? "ChestOpened" : "ChestClosed");
        yield return new WaitForSeconds(chestCooldownTime);
        isBusy = false; 
    }

    public string GetInteractionPrompt()
    {
        if (isBusy) return ""; 
        if (isLocked) return lockedPrompt;
        return isOpen ? closePrompt : openPrompt;
    }
}