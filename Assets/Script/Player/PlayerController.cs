using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 20f;
    
    [Header("Momentum Settings")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f; // New variable to control the check

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
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        wasGrounded = groundCheck();
        isGrounded = wasGrounded;
        
        particleController?.ToggleTrail(isGrounded);
    }

    void Update()
    {
        float verticalVelocityOnImpact = velocity.y;
        
        // Use the method for the per-frame check
        isGrounded = groundCheck();

        if (isGrounded && !wasGrounded)
        {
            float fallIntensity = Mathf.Abs(verticalVelocityOnImpact);
            particleController.ToggleTrail(true);
            particleController.PlayLandEffect(fallIntensity);
            playerAnimation.Land();
            
        }

        if (!isGrounded && wasGrounded)
        {
            particleController.ToggleTrail(false);
        }
        wasGrounded = isGrounded;

        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * z + camRight * x).normalized;

        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerAnimation.Jump();
        }

        // Gravity Logic
        if (velocity.y < 0) { velocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime; }
        else if (velocity.y > 0 && !Input.GetButton("Jump")) { velocity.y += gravity * (lowJumpMultiplier - 1) * Time.deltaTime; }
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -terminalVelocity);

        // Horizontal Momentum
        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * moveSpeed;
        float currentAcceleration = WorldSpaceMoveDirection.magnitude > 0.1f ? acceleration : deceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, currentAcceleration * Time.deltaTime);

        if (horizontalVelocity.magnitude < 0.01f) { horizontalVelocity = Vector3.zero; }

        // Combine and Move
        Vector3 totalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(totalVelocity * Time.deltaTime);
    }

    // This is our single source of truth for ground detection
    private bool groundCheck()
    {
        Vector3 sphereCheckPosition = transform.position + controller.center - Vector3.up * (controller.height / 2);
        return Physics.CheckSphere(sphereCheckPosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }
}