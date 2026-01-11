using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ObjectiveController : MonoBehaviour, IInteractable
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    
    [Header("Audio")]
    [SerializeField] private SoundDefinition sfx_Open;

    [Header("Chest Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    
    [Header("Timing Settings")]
    [SerializeField] private float playerFreezeTime = 1.0f;
    [SerializeField] private float chestCooldownTime = 1.5f; // Time until animation finishes

    [Tooltip("Delay before item starts rising (so lid opens first).")]
    [SerializeField] private float itemRiseDelay = 0.5f; 
    
    [Header("Item Container")]
    [SerializeField] private ObjectiveItemDisplay itemDisplayScript; 

    [Header("Feedback")]
    [SerializeField] private string openPrompt = "Open Chest";
    [SerializeField] private string lockedPrompt = "Locked";
    
    private Animator animator;
    private int animBoolID;
    private bool isBusy = false; 

    void Awake()
    {
        animator = GetComponent<Animator>();
        animBoolID = Animator.StringToHash("IsOpen"); 
        animator.SetBool(animBoolID, isOpen);
        
        if (itemDisplayScript != null)
            itemDisplayScript.SetItemState(isOpen);
    }

    public bool Interact(GameObject interactor)
    {
        // 1. New Condition: If already open, do nothing.
        if (isBusy || isLocked || isOpen) return false;
        
        StartCoroutine(OperationRoutine(interactor));
        return true;
    }

    private IEnumerator OperationRoutine(GameObject interactor)
    {
        isBusy = true; 
        
        // Emit Trace (Noise)
        if(traceChannel != null)
            traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
            
        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null) pc.FreezeInteraction(playerFreezeTime);

        // 2. Set State (One way only)
        isOpen = true;

        // --- OPENING ---
        animator.SetBool(animBoolID, true); 
        
        // Play Open Sound
        if (SoundManager.Instance != null && sfx_Open != null)
        {
            SoundManager.Instance.PlaySound(sfx_Open, transform.position);
        }

        // Wait for lid to clear
        yield return new WaitForSeconds(itemRiseDelay);
        
        // Show Item
        if(itemDisplayScript != null) itemDisplayScript.SetItemState(true);

        InteractionManager.Instance?.ReportInteraction(this.gameObject, "ChestOpened");
        
        yield return new WaitForSeconds(chestCooldownTime);
        isBusy = false; 
    }

    public string GetInteractionPrompt()
    {
        if (isBusy) return ""; 
        if (isLocked) return lockedPrompt;
        
        // 3. If open, return empty string so no UI prompt appears
        if (isOpen) return "";
        
        return openPrompt;
    }
}