using UnityEngine;
using System.Collections; // For coroutines

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform mainCameraTransform;

    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private BoolVariableSO isSprintingState; // Used to tell EnergyManager if player IS sprinting
    [SerializeField] private BoolVariableSO isSlowWalkingState;

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 6f; // Renamed from moveSpeed
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float slowWalkSpeedMultiplier = 0.5f; 
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    // Internal State
    private Vector3 horizontalVelocity = Vector3.zero;
    private float environmentSpeedMultiplier = 1f; 
    private Coroutine slowCoroutine;

    // Public API
    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public float CurrentHorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsMoving => CurrentHorizontalSpeed > 0.01f;

    // Input Booleans (Read directly from InputManager, or use SOs later)
    private bool IsSprintingInput => InputManager.Instance.IsSprinting;
    private bool IsSlowWalkingInput => InputManager.Instance.IsSlowWalking;

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (mainCameraTransform == null) mainCameraTransform = Camera.main.transform;
    }

    public void HandleHorizontalMovement(bool isLocked)
    {
        if (isLocked)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        Vector2 moveInput = InputManager.Instance.MoveInput;

        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; 
        camRight.y = 0;
        camForward.Normalize(); 
        camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        float currentMoveSpeed = baseMoveSpeed;
        bool hasEnergy = currentEnergy.Value > 0;

        if (hasEnergy)
        {
            if (IsSprintingInput) 
            {
                currentMoveSpeed *= sprintSpeedMultiplier;
            }
            else if (IsSlowWalkingInput) 
            {
                currentMoveSpeed *= slowWalkSpeedMultiplier;
            }
        }
        else
        {
            currentMoveSpeed *= slowWalkSpeedMultiplier; // Force slow walk
        }
        
        currentMoveSpeed *= environmentSpeedMultiplier; 

        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * currentMoveSpeed;
        float currentAcceleration = (WorldSpaceMoveDirection.magnitude > 0.1f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, currentAcceleration * Time.deltaTime);
        
        if (horizontalVelocity.magnitude < 0.01f) 
        {
            horizontalVelocity = Vector3.zero;
        }
        
        controller.Move(horizontalVelocity * Time.deltaTime);
        
        // Report sprint status via SO
        if (isSprintingState != null)
        {
            isSprintingState.Value = IsSprintingInput && IsMoving && !isLocked;
        }
        if (isSlowWalkingState != null) // Add this variable to your PlayerMovement script headers first!
        {
            isSlowWalkingState.Value = IsSlowWalkingInput && IsMoving && !isLocked;
        }
    }

    public void ApplyEnvironmentalSlow(float slowFactor, float duration)
    {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(slowFactor, duration));
    }

    private IEnumerator SlowRoutine(float factor, float duration)
    {
        environmentSpeedMultiplier = factor;
        yield return new WaitForSeconds(duration);
        environmentSpeedMultiplier = 1f;
        slowCoroutine = null;
    }
}