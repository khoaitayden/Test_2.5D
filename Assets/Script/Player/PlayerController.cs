using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float slowWalkSpeedMultiplier = 0.5f; // 50% speed when walking
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
    
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalVelocity = Vector3.zero;
    private bool isGrounded;
    private Transform mainCameraTransform;
    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public bool IsSlowWalking { get; private set; }
    private bool wasGrounded;
    private bool currentTrailActive = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        IsSlowWalking = false;
        isGrounded = groundCheck();
        wasGrounded = isGrounded;
        currentTrailActive = isGrounded && !IsSlowWalking; // <-- ADD THIS

        particleController?.ToggleTrail(isGrounded, IsSlowWalking);
    }

    void Update()
    {
        IsSlowWalking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        isGrounded = groundCheck();

        // Handle landing event
        if (isGrounded && !wasGrounded)
        {
            float fallIntensity = Mathf.Abs(velocity.y);
            particleController?.PlayLandEffect(fallIntensity);
            playerAnimation?.Land();
        }

        // Determine desired trail state
        bool desiredTrailState = isGrounded && !IsSlowWalking;

        // Only update if it changed
        if (desiredTrailState != currentTrailActive)
        {
            currentTrailActive = desiredTrailState;
            particleController?.ToggleTrail(isGrounded, IsSlowWalking);
        }

        wasGrounded = isGrounded;

        if (isGrounded && velocity.y < 0) 
        { 
            velocity.y = -2f; 
        }

        // --- Input & Movement Direction ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; 
        camRight.y = 0;
        camForward.Normalize(); 
        camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * z + camRight * x).normalized;

        // Rotate player
        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            particleController.PlayJumpEffect();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerAnimation.Jump();
        }

        // --- Gravity ---
        if (velocity.y < 0) 
        { 
            velocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime; 
        }
        else if (velocity.y > 0 && !Input.GetButton("Jump")) 
        { 
            velocity.y += gravity * (lowJumpMultiplier - 1) * Time.deltaTime; 
        }
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -terminalVelocity);

        // --- Horizontal Movement with Slow Walk ---
        float currentMoveSpeed = moveSpeed;
        if (IsSlowWalking) // Use the property you just set
        {
            currentMoveSpeed *= slowWalkSpeedMultiplier;
        }

        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * currentMoveSpeed;
        float currentAcceleration = WorldSpaceMoveDirection.magnitude > 0.1f ? acceleration : deceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, currentAcceleration * Time.deltaTime);

        if (horizontalVelocity.magnitude < 0.01f) 
        { 
            horizontalVelocity = Vector3.zero; 
        }

        // --- Final Movement ---
        Vector3 totalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(totalVelocity * Time.deltaTime);
    }

    private bool groundCheck()
    {
        Vector3 sphereCheckPosition = transform.position + controller.center - Vector3.up * (controller.height / 2);
        return Physics.CheckSphere(sphereCheckPosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }
}