using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform mainCameraTransform;

    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed; 
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float slowWalkSpeedMultiplier = 0.5f; 
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Auto-Run Settings")]
    [SerializeField] private float autoRunThreshold = 0.9f;
    [SerializeField] private float timeToAutoRun = 1.5f;

    public float sneakSpeed {get; protected set;}
    public float runSpeed {get; protected set;}
    private Vector3 horizontalVelocity = Vector3.zero;
    private float environmentSpeedMultiplier = 1f; 
    private Coroutine slowCoroutine;
    private float _pushDurationTimer;
    private bool _isAutoSprinting;

    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public float CurrentHorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsMoving => CurrentHorizontalSpeed > 0.01f;

    public bool IsSprinting => (InputManager.Instance.IsSprinting || _isAutoSprinting) && IsMoving;

    void Awake()
    {
        sneakSpeed=baseMoveSpeed*slowWalkSpeedMultiplier;
        runSpeed=baseMoveSpeed*sprintSpeedMultiplier;

    }

    public void HandleHorizontalMovement(bool isLocked)
    {
        if (isLocked)
        {
            StopMovement();
            return;
        }

        WorldSpaceMoveDirection = GetCameraRelativeInput();

        HandleRotation();

        float inputMagnitude = InputManager.Instance.MoveInput.magnitude;
        HandleAutoRunLogic(inputMagnitude);

        float targetSpeed = CalculateTargetSpeed(inputMagnitude);
        
        CalculateHorizontalVelocity(targetSpeed);

        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    private void HandleAutoRunLogic(float magnitude)
    {
        if (magnitude >= autoRunThreshold)
        {
            if (!InputManager.Instance.IsSlowWalking)
            {
                _pushDurationTimer += Time.deltaTime;
                if (_pushDurationTimer >= timeToAutoRun)
                {
                    _isAutoSprinting = true;
                }
            }
        }
        else
        {
            _pushDurationTimer = 0f;
            _isAutoSprinting = false;
        }
    }

    private void StopMovement()
    {
        horizontalVelocity = Vector3.zero;
        _pushDurationTimer = 0f;
        _isAutoSprinting = false;
    }

    private Vector3 GetCameraRelativeInput()
    {
        Vector2 input = InputManager.Instance.MoveInput;
        // Lowered threshold slightly to make it more responsive
        if (input.sqrMagnitude < 0.001f) return Vector3.zero;

        Vector3 camFwd = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camFwd.y = 0;
        camRight.y = 0;
        
        // Use normalized here for direction, but we use magnitude later for speed
        return (camFwd.normalized * input.y + camRight.normalized * input.x).normalized;
    }

    private void HandleRotation()
    {
        if (WorldSpaceMoveDirection.sqrMagnitude >= 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private float CalculateTargetSpeed(float inputMagnitude)
    {
        float speed = baseMoveSpeed;

        bool hasEnergy = currentEnergy != null && currentEnergy.Value > 0;

        if (!hasEnergy)
        {
            speed *= slowWalkSpeedMultiplier;
        }
        else if (IsSprinting) 
        {
            speed *= sprintSpeedMultiplier;
        }
        else if (InputManager.Instance.IsSlowWalking)
        {
            speed *= slowWalkSpeedMultiplier;
        }

        speed *= Mathf.Clamp01(inputMagnitude);

        return speed * environmentSpeedMultiplier;
    }

    private void CalculateHorizontalVelocity(float targetSpeed)
    {
        Vector3 targetVel = WorldSpaceMoveDirection * targetSpeed;

        float accelRate = (WorldSpaceMoveDirection.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVel, accelRate * Time.deltaTime);

        if (horizontalVelocity.sqrMagnitude < 0.01f) horizontalVelocity = Vector3.zero;
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