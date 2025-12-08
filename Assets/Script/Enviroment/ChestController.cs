using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Chest Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    
    [Header("Timing Settings")]
    [Tooltip("How long the player is frozen (lifting heavy lid).")]
    [SerializeField] private float playerFreezeTime = 0.5f; // Chests usually feel heavier/slower than doors

    [Tooltip("How long to wait before it can be used again.")]
    [SerializeField] private float chestCooldownTime = 1.5f;
    
    [Header("Conditions")]
    [SerializeField] private string requiredKeyName = ""; 

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
        // Assumes your Animator has a Bool parameter named "IsOpen"
        animBoolID = Animator.StringToHash("IsOpen"); 
        animator.SetBool(animBoolID, isOpen);
    }

    public bool Interact(GameObject interactor)
    {
        // 1. REJECT SPAM
        if (isBusy) return false;

        // 2. CHECK LOCK
        if (isLocked)
        {
            // Future inventory check logic goes here
            // return false; 
        }

        // 3. START SEQUENCE
        StartCoroutine(OperationRoutine(interactor));
        return true;
    }

    private IEnumerator OperationRoutine(GameObject interactor)
    {
        isBusy = true; 
        
        // Emit noise (Chests are squeaky!)
        TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseMedium);

        // 1. FREEZE PLAYER
        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.FreezeInteraction(playerFreezeTime);
        }

        // 2. TOGGLE STATE
        isOpen = !isOpen;
        animator.SetBool(animBoolID, isOpen);
        
        // Report event (Useful for cutscenes or tutorials later)
        InteractionManager.Instance?.ReportInteraction(this.gameObject, isOpen ? "ChestOpened" : "ChestClosed");

        // 3. WAIT FOR ANIMATION/COOLDOWN
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