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
    [SerializeField] private LayerMask groundLayer;

    [Header("Momentum Settings")]
    [SerializeField] private float acceleration = 10f;    // How fast you reach max speed
    [SerializeField] private float deceleration = 15f;    // How fast you stop

    [Header("Advanced Gravity")]
    [Tooltip("Multiplier for gravity when the player is falling.")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [Tooltip("Multiplier for gravity when the player releases the jump button early.")]
    [SerializeField] private float lowJumpMultiplier = 2f;
    [Tooltip("The maximum speed the player can fall at.")]
    [SerializeField] private float terminalVelocity = 50f;

    [Header("Reference")]
    [SerializeField] private PlayerParticleController particleController;
    [SerializeField] private PlayerAnimation playerAnimation;
    
    private CharacterController controller;
    private Vector3 velocity; // only used for Y (gravity/jump)
    private Vector3 horizontalVelocity = Vector3.zero; // X/Z momentum
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
        wasGrounded = true;
        isGrounded = false;
    }

    void Update()
    {
        float verticalVelocityOnImpact = velocity.y;
        isGrounded = Physics.CheckSphere(transform.position + controller.center - new Vector3(0, controller.height / 2, 0), 0.2f, groundLayer, QueryTriggerInteraction.Ignore);
        
        if (isGrounded && !wasGrounded)
        {
            float fallIntensity = Mathf.Abs(verticalVelocityOnImpact);
            particleController.PlayLandEffect(fallIntensity);
            playerAnimation.Land();
        }
        wasGrounded = isGrounded;
        particleController.ToggleDirtTrail(isGrounded);
        
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

        // --- Gravity Logic (unchanged) ---
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

        // --- HORIZONTAL MOMENTUM ---
        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * moveSpeed;

        if (WorldSpaceMoveDirection.magnitude > 0.1f)
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, 
                acceleration * Time.deltaTime);
        }
        else
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, 
                deceleration * Time.deltaTime);
        }

        // Prevent tiny drift
        if (horizontalVelocity.magnitude < 0.1f) 
            horizontalVelocity = Vector3.zero;

        // Combine horizontal + vertical
        Vector3 totalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(totalVelocity * Time.deltaTime);
    }
}