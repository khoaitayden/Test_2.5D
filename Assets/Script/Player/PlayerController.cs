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
    private bool isInteractionLocked; // New Flag for Door Animation

    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public bool IsDead => isDead; // Public getter for other scripts (like Interactor)
    public bool IsInteractionLocked => isInteractionLocked;

    // Properties strictly for logic/animation
    private bool IsSlowWalking => InputManager.Instance.IsSlowWalking;
    private bool IsSprinting => InputManager.Instance.IsSprinting;
    
    private bool wasGrounded;
    private bool jumpRequest; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        
        // Ensure cursor is locked at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        isDead = false;
        isInteractionLocked = false;

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

    // --- New Method: Called by DoorController ---
    public void FreezeInteraction(float duration)
    {
        if (!isDead) StartCoroutine(LockMovementRoutine(duration));
    }

    private IEnumerator LockMovementRoutine(float duration)
    {
        isInteractionLocked = true;
        horizontalVelocity = Vector3.zero; // Kill momentum immediately
        yield return new WaitForSeconds(duration);
        isInteractionLocked = false;
    }

    private void HandleJumpTrigger()
    {
        // Don't jump if locked or dead
        if (isInteractionLocked || isDead) return;

        if(isGrounded) jumpRequest = true;
        // TraceEventBus.Emit(transform.position, TraceType.Footstep_Jump);
    }

    void Update()
    {
        // 1. GRAVITY & GROUND CHECK (Always runs so player falls even when dead)
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

        // 2. STOP LOGIC IF DEAD OR LOCKED
        if (isDead || isInteractionLocked)
        {
            // Apply only gravity, no movement
            ApplyGravity();
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            return; // EXIT HERE
        }

        // 3. NORMAL MOVEMENT LOGIC
        HandleMovement();
    }

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
        
        // Zero out momentum so body doesn't slide
        horizontalVelocity = Vector3.zero; 
        
        // ENABLE CURSOR FOR UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        uIManager.ToggleDeathScreen();
    }
}