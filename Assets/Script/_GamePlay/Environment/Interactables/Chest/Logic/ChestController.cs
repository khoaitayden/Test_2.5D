using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    
    // --- 1. NEW AUDIO FIELDS ---
    [Header("Audio")]
    [SerializeField] private SoundDefinition sfx_Open;
    // --------------------------

    [Header("Chest Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    
    [Header("Timing Settings")]
    [SerializeField] private float playerFreezeTime = 1.0f;
    [SerializeField] private float chestCooldownTime = 1.5f;

    [Tooltip("Delay before item starts rising (so lid opens first).")]
    [SerializeField] private float itemRiseDelay = 0.5f; 
    
    [Header("Item Container")]
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
        
        // Emit Trace (Noise)
        if(traceChannel != null)
            traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
            
        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null) pc.FreezeInteraction(playerFreezeTime);

        // Toggle State
        isOpen = !isOpen;

        if (isOpen)
        {
            // --- OPENING ---
            animator.SetBool(animBoolID, true); 
            
            // 2. PLAY OPEN SOUND
            if (SoundManager.Instance != null && sfx_Open != null)
            {
                SoundManager.Instance.PlaySound(sfx_Open, transform.position);
            }

            yield return new WaitForSeconds(itemRiseDelay);
            
            if(itemDisplayScript != null) itemDisplayScript.SetItemState(true);
        }
        else
        {
            // --- CLOSING ---
            if(itemDisplayScript != null) itemDisplayScript.SetItemState(false);
            
            yield return new WaitForSeconds(0.3f); 
            
            animator.SetBool(animBoolID, false); 
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