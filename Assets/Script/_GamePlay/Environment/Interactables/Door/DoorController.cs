using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; // Drag "channel_TraceEvents"

    [Header("Audio")]
    [SerializeField] private SoundDefinition sfx_Open;
    [SerializeField] private SoundDefinition sfx_Close;

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
    private bool isBusy = false; 

    void Awake()
    {
        animator = GetComponent<Animator>();
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
            // Optional: Play "Locked" sound here?
            return false; 
        }

        // 3. START SEQUENCE
        StartCoroutine(OperationRoutine(interactor));
        return true;
    }

    private IEnumerator OperationRoutine(GameObject interactor)
    {
        isBusy = true; 
        
        // 1. EMIT NOISE (Medium radius because doors squeak/bang)
        if (traceChannel != null)
            traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);

        // 2. FREEZE PLAYER
        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.FreezeInteraction(playerFreezeTime);
        }

        // 3. PERFORM TOGGLE
        isOpen = !isOpen;
        animator.SetBool(animBoolID, isOpen);
        
        // 4. PLAY SOUND
        if (SoundManager.Instance != null)
        {
            SoundDefinition clipToPlay = isOpen ? sfx_Open : sfx_Close;
            if (clipToPlay != null)
            {
                SoundManager.Instance.PlaySound(clipToPlay, transform.position);
            }
        }
        
        InteractionManager.Instance?.ReportInteraction(this.gameObject, isOpen ? "DoorOpened" : "DoorClosed");

        // 5. WAIT FOR COOLDOWN
        yield return new WaitForSeconds(doorCooldownTime);
        
        isBusy = false; 
    }

    public string GetInteractionPrompt()
    {
        if (isBusy) return "";
        if (isLocked) return lockedPrompt;
        return isOpen ? closePrompt : openPrompt;
    }
}