using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerGroundedChecker playerGroundedChecker;
    [SerializeField] private PlayerClimbing playerClimbing;
    [SerializeField] private PlayerState playerState;
    
    // Architectures
    [SerializeField] private TransformAnchorSO playerAnchor;
    [SerializeField] private TraceEventChannelSO traceChannel; 
    
    // State Flags (Owned by PlayerController)
    private bool isInteractionLocked; 
    private Coroutine lockCoroutine; 

    // Public API (Forwarded from sub-components)
    public bool IsDead => playerState.IsDead; 
    public bool IsInteractionLocked => isInteractionLocked;
    public bool IsClimbing => playerClimbing.IsClimbing;
    public bool IsEnteringLadder => playerClimbing.IsEnteringLadder;
    public Vector3 WorldSpaceMoveDirection => playerMovement.WorldSpaceMoveDirection;
    public float CurrentHorizontalSpeed => playerMovement.CurrentHorizontalSpeed;

    void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerGroundedChecker == null) playerGroundedChecker = GetComponent<PlayerGroundedChecker>();
        if (playerClimbing == null) playerClimbing = GetComponent<PlayerClimbing>();
        if (playerState == null) playerState = GetComponent<PlayerState>();
    }

    void Start()
    {
        if (playerAnchor != null) playerAnchor.Provide(this.transform);
        
        // Subscribe to input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered += HandleJumpRequest;
        }

        // Initial setup
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // REMOVED: playerParticleController.ToggleTrail - ParticleController does this itself now!
    }

    void OnDestroy()
    {
        if (playerAnchor != null) playerAnchor.Unset();
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered -= HandleJumpRequest;
        }
    }

    private void HandleJumpRequest()
    {
        // PlayerGroundedChecker will handle the actual jump physics.
        // We just emit a trace here IF the jump is allowed.
        if (!IsClimbing && !IsEnteringLadder && !IsInteractionLocked && !IsDead)
        {
            if (playerGroundedChecker.IsGrounded && traceChannel != null)
            {
                traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
            }
        }
    }
    

    public void FreezeInteraction(float duration)
    {
        if (IsDead) return; 
        if (lockCoroutine != null) StopCoroutine(lockCoroutine);
        lockCoroutine = StartCoroutine(LockMovementRoutine(duration));
    }

    private IEnumerator LockMovementRoutine(float duration)
    {
        isInteractionLocked = true;
        yield return new WaitForSeconds(duration);
        isInteractionLocked = false;
    }

    void Update()
    {
        
        // Core state management
        if (IsDead)
        {
            playerGroundedChecker.ApplyGravityAndJump(false); 
            return;
        }

        if (IsInteractionLocked || IsEnteringLadder)
        {
            playerGroundedChecker.ApplyGravityAndJump(false); 
            playerMovement.HandleHorizontalMovement(true); // Stop movement
            return;
        }

        // Order of operations:
        playerClimbing.TryStartClimbing(isInteractionLocked);
        if (playerClimbing.IsClimbing)
        {
            playerClimbing.HandleClimbingPhysics();
            
            playerGroundedChecker.SetVerticalVelocity(0f); 
        }
        else
        {
            // --- NORMAL WALKING STATE ---
            playerMovement.HandleHorizontalMovement(isInteractionLocked);
            playerGroundedChecker.ApplyGravityAndJump(true);
        }
        
        // REMOVED: playerParticleController.ToggleTrail - It updates itself in its own Update loop now.
    }
}