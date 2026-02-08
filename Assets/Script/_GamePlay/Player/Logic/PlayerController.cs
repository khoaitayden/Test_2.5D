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
    
    [SerializeField] private TransformAnchorSO playerAnchor;
    [SerializeField] private TraceEventChannelSO traceChannel; 

    private bool isInteractionLocked; 
    private Coroutine lockCoroutine; 

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

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered += HandleJumpRequest;
        }
        
        Cursor.lockState = CursorLockMode.Confined; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
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
            playerMovement.HandleHorizontalMovement(true);
            return;
        }
        playerClimbing.TryStartClimbing(isInteractionLocked);
        if (playerClimbing.IsClimbing)
        {
            playerClimbing.HandleClimbingPhysics();
            playerGroundedChecker.SetVerticalVelocity(0f);
        }
        else
        {
            playerMovement.HandleHorizontalMovement(isInteractionLocked);
            playerGroundedChecker.ApplyGravityAndJump(true);
        }
    }
}