using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeedMultiplier;
    [SerializeField] private float slowWalkSpeedMultiplier = 0.5f; 
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 20f;
    
    [Header("Climbing Settings")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float climbJumpForce = 5f;
    [SerializeField] private float nextClimbTime = 5f; 
    [SerializeField] private float ladderSnapSpeed = 5f; 

    [Header("Momentum Settings")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Advanced Gravity")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float terminalVelocity = 50f;

    [Header("Reference")]
    [SerializeField] private PlayerParticleController particleController;
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private UIManager uIManager;
    
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalVelocity = Vector3.zero;
    private bool isGrounded;
    private Transform mainCameraTransform;
    
    // State Flags
    private bool isDead;
    private bool isInteractionLocked; 
    private bool isClimbing; // NEW STATE

    // Ladder Reference
    private Ladder nearbyLadder;

    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public bool IsDead => isDead; 
    public bool IsInteractionLocked => isInteractionLocked;
    public bool IsClimbing => isClimbing;

    private bool IsSlowWalking => InputManager.Instance.IsSlowWalking;
    private bool IsSprinting => InputManager.Instance.IsSprinting;
    
    private bool wasGrounded;
    private bool jumpRequest; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        isDead = false;
        isInteractionLocked = false;
        isClimbing = false;

        isGrounded = groundCheck();
        wasGrounded = isGrounded;

        particleController?.ToggleTrail(isGrounded, IsSlowWalking);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered += HandleJumpTrigger;
        }
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered -= HandleJumpTrigger;
        }
    }

    // --- Ladder API ---
    public void SetLadderNearby(Ladder ladder)
    {
        nearbyLadder = ladder;
    }

    public void ClearLadderNearby()
    {
        nearbyLadder = null;
        if (isClimbing) StopClimbing();
    }

    // --- Interaction Locking ---
    public void FreezeInteraction(float duration)
    {
        if (!isDead) StartCoroutine(LockMovementRoutine(duration));
    }

    private IEnumerator LockMovementRoutine(float duration)
    {
        isInteractionLocked = true;
        horizontalVelocity = Vector3.zero; 
        yield return new WaitForSeconds(duration);
        isInteractionLocked = false;
    }

    private void HandleJumpTrigger()
    {
        if (isInteractionLocked || isDead) return;

        // If climbing, jump detaches the player
        if (isClimbing)
        {
            StopClimbing();
            // Optional: Add a little push away/up
            velocity.y = Mathf.Sqrt(climbJumpForce * -2f * gravity);
            // Push backwards slightly
            horizontalVelocity = -transform.forward * 2f;
            return;
        }
        if (isGrounded){ 
            jumpRequest = true;
            TraceEventBus.Emit(transform.position, TraceType.Footstep_Jump);
        }
    }

    void Update()
    {
        // 1. STATE CHECK: DEAD OR LOCKED
        if (isDead || isInteractionLocked)
        {
            isGrounded = groundCheck();
            ApplyGravity();
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            return;
        }

        // 2. STATE CHECK: CLIMBING
        // Check if we should START climbing (Near ladder + Pressing Forward/Up)
        if (!isClimbing && nearbyLadder != null && Time.time > nextClimbTime)
        {
            if (InputManager.Instance.MoveInput.y > 0.1f)
            {
                StartClimbing();
            }
        }

        if (isClimbing)
        {
            HandleClimbing();
            return; // Skip normal movement
        }

        // 3. NORMAL MOVEMENT
        isGrounded = groundCheck();

        if (isGrounded && !wasGrounded)
        {
            float fallIntensity = Mathf.Abs(velocity.y);
            particleController?.PlayLandEffect(fallIntensity);
            particleController?.ToggleTrail(isGrounded, IsSlowWalking);
            playerAnimation?.Land();
        }
        wasGrounded = isGrounded;

        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }

        HandleMovement();
    }

    // --- CLIMBING LOGIC ---

   
    private void StartClimbing()
    {
        isClimbing = true;
        velocity = Vector3.zero;        // Stop falling
        horizontalVelocity = Vector3.zero; // Stop sliding
        
        // Note: We DO NOT teleport the player here anymore.
        // We will smooth them into position in HandleClimbing.
    }

    private void StopClimbing()
    {
        isClimbing = false;
        // Cooldown prevents immediate re-grab while still inside the trigger
        nextClimbTime = Time.time + 0.5f; 
    }

    private void HandleClimbing()
    {
        Vector2 input = InputManager.Instance.MoveInput;

        // --- 1. HANDLE EXIT: JUMP (Free Movement) ---
        if (InputManager.Instance.IsJumpHeld) // Trigger jump
        {
            StopClimbing();

            // Velocity Y: Standard Jump Up
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); 

            // Velocity Horizontal: Base it on Input!
            // No input = Jump Backwards.
            // Input = Jump towards WASD direction.
            if (input.sqrMagnitude > 0.01f)
            {
                // Convert input to world direction based on where we are facing
                Vector3 jumpDir = (transform.forward * input.y + transform.right * input.x).normalized;
                horizontalVelocity = jumpDir * (moveSpeed * 0.8f);
            }
            else
            {
                // Default: Jump Back/Away from wall
                horizontalVelocity = -transform.forward * 3f;
            }

            // Apply immediately this frame
            controller.Move((horizontalVelocity + Vector3.up * velocity.y) * Time.deltaTime);
            return;
        }

        // --- 2. SMOOTH SNAP TO LADDER (Fixing "Glitchy Snap") ---
        if (nearbyLadder != null)
        {
            // Rotation: Smoothly face the ladder
            Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // Position: Calculate the "Perfect" position on the ladder (X and Z)
            // But keep our current Y height
            Vector3 ladderPos = nearbyLadder.transform.position;
            Vector3 idealPos = new Vector3(ladderPos.x, transform.position.y, ladderPos.z);
            
            // Offset back by radius + small buffer so we aren't Inside the mesh
            idealPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.2f);

            // Lerp Character towards that X/Z line. 
            // We create a vector from current pos to ideal pos.
            Vector3 toIdeal = idealPos - transform.position;
            // Zero out Y because vertical movement handles that
            toIdeal.y = 0; 
            
            // Apply this adjustment motion
            controller.Move(toIdeal * ladderSnapSpeed * Time.deltaTime);
        }

        // --- 3. VERTICAL MOVEMENT ---
        Vector3 verticalMove = Vector3.up * (input.y * climbSpeed);
        controller.Move(verticalMove * Time.deltaTime);

        // --- 4. CHECK EXITS (Top and Bottom) ---

        // Check feet position relative to Ladder Top
        float feetY = transform.position.y - (controller.height * 0.5f);
        
        // TOP EXIT:
        if (input.y > 0 && feetY >= nearbyLadder.GetLadderTopY())
        {
            StopClimbing();

            // Vault Logic:
            // Give a tiny Pop Up so we don't catch feet on edge
            velocity.y = 2.0f; 
            // Give forward momentum so we land on floor
            horizontalVelocity = transform.forward * moveSpeed; 
            return;
        }

        // BOTTOM EXIT:
        if (input.y < 0)
        {
            // If touching ground while holding Down
            if ((controller.collisionFlags & CollisionFlags.Below) != 0)
            {
                StopClimbing();
            }
        }
    }

    // --- NORMAL MOVEMENT LOGIC ---

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;

        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (jumpRequest && isGrounded)
        {
            particleController?.PlayJumpEffect();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerAnimation?.Jump();
            jumpRequest = false; 
        }

        ApplyGravity();
        
        float currentMoveSpeed = moveSpeed;
        if (IsSprinting) { currentMoveSpeed *= sprintSpeedMultiplier; }
        else if (IsSlowWalking) { currentMoveSpeed *= slowWalkSpeedMultiplier; }

        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * currentMoveSpeed;
        float currentAcceleration = WorldSpaceMoveDirection.magnitude > 0.1f ? acceleration : deceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, currentAcceleration * Time.deltaTime);
        
        if (horizontalVelocity.magnitude < 0.01f) { horizontalVelocity = Vector3.zero; }
        
        Vector3 totalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(totalVelocity * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (velocity.y < 0) 
        { 
            velocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime; 
        }
        else if (velocity.y > 0 && !InputManager.Instance.IsJumpHeld) 
        { 
            velocity.y += gravity * (lowJumpMultiplier - 1) * Time.deltaTime; 
        }
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -terminalVelocity);
    }

    private bool groundCheck()
    {
        Vector3 sphereCheckPosition = transform.position + controller.center - Vector3.up * (controller.height / 2);
        return Physics.CheckSphere(sphereCheckPosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead == false && other.CompareTag("Monster"))
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Killed");
        isDead = true;
        horizontalVelocity = Vector3.zero; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        uIManager.ToggleDeathScreen();
    }
}