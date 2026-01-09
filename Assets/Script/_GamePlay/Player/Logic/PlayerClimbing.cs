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

        // 1. Orient Player
        RotateTowardsLadder();

        // 2. Calculate Movement
        float vInput = InputManager.Instance.MoveInput.y;
        Vector3 finalVelocity = CalculateClimbingVelocity(vInput);

        // 3. Enforce Limits (Prevents climbing over the top)
        finalVelocity = EnforceHeadLimit(finalVelocity, vInput);

        // 4. Apply
        controller.Move(finalVelocity * Time.deltaTime);

        // 5. Check Exits
        CheckExits(vInput);
    }

    // --- HELPER METHODS ---

    private void RotateTowardsLadder()
    {
        Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    private Vector3 CalculateClimbingVelocity(float vInput)
    {
        Vector3 velocity = Vector3.zero;

        // A. Vertical (Input based)
        velocity.y = vInput * climbSpeed;

        // B. Horizontal (Suction towards ladder center)
        Vector3 targetPos = nearbyLadder.GetClosestPointOnLadder(transform.position);
        targetPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.1f);
        
        Vector3 moveDir = (targetPos - transform.position);
        moveDir.y = 0; // Important: Keep Y pure

        velocity.x = moveDir.x * climbSnapSpeed;
        velocity.z = moveDir.z * climbSnapSpeed;

        return velocity;
    }

    private Vector3 EnforceHeadLimit(Vector3 currentVelocity, float vInput)
    {
        // If going down, no limits needed
        if (vInput <= 0) return currentVelocity;

        float currentHeadY = transform.position.y + controller.center.y + (controller.height * 0.5f);
        float maxHeadY = nearbyLadder.TopY + 0.1f;

        if (currentHeadY >= maxHeadY)
        {
            // Kill Vertical Velocity
            currentVelocity.y = 0;

            // Hard Clamp Transform to fix overshoots
            SnapPositionToLimit(maxHeadY);
        }

        return currentVelocity;
    }

    private void SnapPositionToLimit(float maxHeadY)
    {
        float requiredY = maxHeadY - (controller.center.y + (controller.height * 0.5f));
        if (Mathf.Abs(transform.position.y - requiredY) > 0.001f)
        {
            Vector3 clampedPos = transform.position;
            clampedPos.y = requiredY;
            transform.position = clampedPos;
        }
    }
    private void CheckExits(float vInput)
    {
        // Condition A: Jump / Vault (Manual Exit)
        if (InputManager.Instance.IsJumpHeld)
        {
            PerformDirectionalDismount();
            return;
        }

        if (vInput < 0) 
        {
            if (groundedChecker.CheckGround())
            {
                StopClimbing();
                return;
            }
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