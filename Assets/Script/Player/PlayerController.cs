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
    private bool isDead;
    public Vector3 WorldSpaceMoveDirection { get; private set; }
    
    // Properties strictly for logic/animation
    private bool IsSlowWalking => InputManager.Instance.IsSlowWalking;
    private bool IsSprinting => InputManager.Instance.IsSprinting;
    
    private bool wasGrounded;
    
    // Jump specific flags
    private bool jumpRequest; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isDead = false;

        isGrounded = groundCheck();
        wasGrounded = isGrounded;

        particleController?.ToggleTrail(isGrounded, IsSlowWalking);

        // Subscribe to Jump Event from InputManager
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

    private void HandleJumpTrigger()
    {
        if(isGrounded) jumpRequest = true;
    }

    void Update()
    {
        isGrounded = groundCheck();
        
        // --- Landing Logic ---
        if (isGrounded && !wasGrounded)
        {
            float fallIntensity = Mathf.Abs(velocity.y);
            particleController?.PlayLandEffect(fallIntensity);
            particleController?.ToggleTrail(isGrounded, IsSlowWalking);
            playerAnimation?.Land();
        }
        
        wasGrounded = isGrounded;

        // Reset gravity accumulation when grounded
        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }
        
        // --- Movement Calculation ---
        // Get Input directly from Manager
        Vector2 moveInput = InputManager.Instance.MoveInput;

        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        // Rotation
        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Jumping ---
        if (jumpRequest && isGrounded)
        {
            particleController?.PlayJumpEffect();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerAnimation?.Jump();
            jumpRequest = false; // Reset request
        }
        
        // --- Gravity ---
        if (velocity.y < 0) 
        { 
            velocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime; 
        }
        else if (velocity.y > 0 && !InputManager.Instance.IsJumpHeld) // Check held state for variable jump
        { 
            velocity.y += gravity * (lowJumpMultiplier - 1) * Time.deltaTime; 
        }
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -terminalVelocity);
        
        // --- Velocity Application ---
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

    private bool groundCheck()
    {
        Vector3 sphereCheckPosition = transform.position + controller.center - Vector3.up * (controller.height / 2);
        return Physics.CheckSphere(sphereCheckPosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead == false && other.CompareTag("Monster"))
        {
            Debug.Log("Killed");
            isDead = true;
            uIManager.ToggleDeathScreen();
        }
    }
}