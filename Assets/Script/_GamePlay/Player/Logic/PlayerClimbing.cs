using UnityEngine;

public class PlayerClimbing : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerGroundedChecker groundedChecker;
    [SerializeField] private PlayerMovement playerMovement; // Added to get Move Direction

    [Header("Settings")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float climbSnapSpeed = 5f; 
    
    [Header("Dismount Settings")]
    [Tooltip("How much force is applied horizontally when jumping off.")]
    [SerializeField] private float jumpOffForceHorizontal = 4f;
    [Tooltip("How much force is applied upward when jumping off.")]
    [SerializeField] private float jumpOffForceUp = 5f;

    // State
    private Ladder nearbyLadder;
    private bool isClimbing;
    private float cooldownTimer;

    // Public API
    public bool IsClimbing => isClimbing;
    public bool IsEnteringLadder => false; 

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    public void SetLadderNearby(Ladder ladder) => nearbyLadder = ladder;
    
    public void ClearLadderNearby()
    {
        nearbyLadder = null;
        if (isClimbing) StopClimbing();
    }

    public void TryStartClimbing(bool isMovementLocked)
    {
        if (isMovementLocked || isClimbing || nearbyLadder == null) return;
        if (Time.time < cooldownTimer) return;

        float vInput = InputManager.Instance.MoveInput.y;
        if (Mathf.Abs(vInput) > 0.1f)
        {
            isClimbing = true;
        }
    }

    public void HandleClimbingPhysics()
    {
        if (!isClimbing || nearbyLadder == null) 
        {
            StopClimbing();
            return;
        }

        // 1. ROTATION: Face the ladder smoothly
        Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);

        // 2. CHECK POSITION (Are we at the top?)
        float feetY = transform.position.y - (controller.height * 0.5f);
        // Allow a small buffer (0.1f) so we don't snap out of existence
        bool atTop = feetY >= nearbyLadder.TopY - 0.1f; 

        // 3. CALCULATE VELOCITY
        Vector3 finalVelocity = Vector3.zero;
        float vInput = InputManager.Instance.MoveInput.y;

        // A. Vertical Movement
        if (atTop && vInput > 0)
        {
            // HARD STOP at the top
            finalVelocity.y = 0;
        }
        else
        {
            finalVelocity.y = vInput * climbSpeed;
        }

        // B. Horizontal Alignment (Suction towards ladder center)
        Vector3 targetPos = nearbyLadder.GetClosestPointOnLadder(transform.position);
        targetPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.1f);
        Vector3 moveDir = (targetPos - transform.position);
        finalVelocity.x = moveDir.x * climbSnapSpeed;
        finalVelocity.z = moveDir.z * climbSnapSpeed;

        // 4. APPLY MOVEMENT
        controller.Move(finalVelocity * Time.deltaTime);

        // 5. CHECK EXITS
        CheckExits(vInput);
    }

    private void CheckExits(float vInput)
    {
        // Condition A: Jump / Vault (Manual Exit)
        if (InputManager.Instance.IsJumpHeld) // Using Held to ensure responsiveness, logic ensures single trigger
        {
            PerformDirectionalDismount();
            return;
        }

        // Condition B: Grounded (Walked off bottom) - Automatic Exit
        if (vInput < 0 && groundedChecker.IsGrounded)
        {
            StopClimbing();
            return;
        }
    }

    private void PerformDirectionalDismount()
    {
        StopClimbing();

        // 1. Determine Direction
        // We use PlayerMovement because it already calculates direction relative to Camera
        Vector3 vaultDir = playerMovement.WorldSpaceMoveDirection;

        // If player is not pressing any keys, default to jumping BACKWARDS away from ladder
        if (vaultDir.magnitude < 0.1f)
        {
            vaultDir = -transform.forward;
        }

        // 2. Apply Force
        // Horizontal push
        Vector3 jumpForce = (vaultDir.normalized * jumpOffForceHorizontal) + (Vector3.up * jumpOffForceUp);
        
        // Move immediately so we clear the ladder collider/trigger area
        controller.Move(jumpForce * Time.deltaTime); 
        
        // 3. Hand over vertical momentum to Gravity System
        groundedChecker.SetVerticalVelocity(jumpOffForceUp);
    }

    private void StopClimbing()
    {
        isClimbing = false;
        cooldownTimer = Time.time + 0.5f; // Delay before we can grab it again
    }
}