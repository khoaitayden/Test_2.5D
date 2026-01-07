using UnityEngine;
using UnityEngine.Events; // For events like OnLand

[RequireComponent(typeof(CharacterController))]
public class PlayerGroundedChecker : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController controller;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float terminalVelocity = 50f;

    [Header("Events")]
    [SerializeField] private GameEventSO onJumpEvent;   // Create this SO: "evt_PlayerJump"
    [SerializeField] private GameEventSO onLandEvent;   // Create this SO: "evt_PlayerLand"
    
    // For PlayerAudio/ParticleController to listen directly
    public UnityAction<float> OnLandWithFallIntensity; // Raw C# event for specific data

    // Internal State
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpRequest;

    // Public API
    public bool IsGrounded => isGrounded;
    public Vector3 VerticalVelocity => verticalVelocity;

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
    }
    void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered += HandleJumpTrigger;
            InputManager.Instance.OnJumpReleased += HandleJumpReleased;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpTriggered -= HandleJumpTrigger;
            InputManager.Instance.OnJumpReleased -= HandleJumpReleased;
        }
    }

    private void HandleJumpTrigger()
    {
        if (IsGrounded)
        {
            jumpRequest = true;
        }
    }

    private void HandleJumpReleased()
    {
        // For variable jump height
    }

    public void ApplyGravityAndJump(bool allowJump)
    {
        isGrounded = CheckGround();

        if (isGrounded && !wasGrounded)
        {
            // Landing event
            float fallIntensity = Mathf.Abs(verticalVelocity.y);
            OnLandWithFallIntensity?.Invoke(fallIntensity); // Raw event with data
            if (onLandEvent != null) onLandEvent.Raise();  // Generic event without data
        }
        wasGrounded = isGrounded;

        if (isGrounded && verticalVelocity.y < 0) 
        { 
            verticalVelocity.y = -2f; 
        }

        if (allowJump && jumpRequest && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (onJumpEvent != null) onJumpEvent.Raise();
            jumpRequest = false; 
        }

        ApplyGravityCalculation();
        controller.Move(Vector3.up * verticalVelocity.y * Time.deltaTime);
    }

    private void ApplyGravityCalculation()
    {
        if (verticalVelocity.y < 0) 
        { 
            verticalVelocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime; 
        }
        else if (verticalVelocity.y > 0 && !InputManager.Instance.IsJumpHeld) 
        { 
            verticalVelocity.y += gravity * (lowJumpMultiplier - 1) * Time.deltaTime; 
        }
        verticalVelocity.y += gravity * Time.deltaTime;
        verticalVelocity.y = Mathf.Max(verticalVelocity.y, -terminalVelocity);
    }

    private bool CheckGround()
    {
        Vector3 sphereCheckPosition = transform.position + controller.center - Vector3.up * (controller.height / 2);
        return Physics.CheckSphere(sphereCheckPosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }
}