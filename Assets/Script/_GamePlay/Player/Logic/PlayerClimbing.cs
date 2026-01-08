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

        // 1. ROTATION
        Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        float currentHeadY = transform.position.y + controller.center.y + (controller.height * 0.5f);
        float ladderTopY = nearbyLadder.TopY;
        
        float maxHeadY = ladderTopY + 0.1f; 

        // 3. INPUT
        float vInput = InputManager.Instance.MoveInput.y;
        Vector3 finalVelocity = Vector3.zero;

        // 4. VERTICAL LOGIC
        // Check if we are trying to go UP past the limit
        if (currentHeadY >= maxHeadY && vInput > 0)
        {
            finalVelocity.y = 0;
            float requiredY = maxHeadY - (controller.center.y + (controller.height * 0.5f));
            
            Vector3 clampedPos = transform.position;
            clampedPos.y = requiredY;
            transform.position = clampedPos;
        }
        else
        {
            finalVelocity.y = vInput * climbSpeed;
        }

        // 5. HORIZONTAL SUCTION
        Vector3 targetPos = nearbyLadder.GetClosestPointOnLadder(transform.position);
        targetPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.1f);
        Vector3 moveDir = (targetPos - transform.position);
        moveDir.y = 0; // Vital: Keep Y pure

        finalVelocity.x = moveDir.x * climbSnapSpeed;
        finalVelocity.z = moveDir.z * climbSnapSpeed;

        // 6. APPLY
        controller.Move(finalVelocity * Time.deltaTime);

        // 7. EXITS
        CheckExits(vInput);
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