using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    [Header("Data")]
    [SerializeField] private BoolVariableSO isFlashlightOn;
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [SerializeField] private BoolVariableSO isSprinting;
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeedMultiplier;
    [SerializeField] private float slowWalkSpeedMultiplier = 0.5f; 
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 20f;

    [Header("Climbing Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [Tooltip("How long the smooth transition ONTO the ladder takes.")]
    [SerializeField] private float ladderEntryDuration = 0.3f;
    [Tooltip("How far from the top of the ladder the player will stop climbing.")]
    [SerializeField] private float climbTopOffset = 0.5f;
    [Tooltip("The UPWARD force when jumping off the top.")]
    [SerializeField] private float topDismountUpwardForce = 10f;
    [Tooltip("The BACKWARD force to clear the ladder when jumping off the top.")]
    [SerializeField] private float topDismountBackwardForce = 3f;

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
    [SerializeField] private PlayerAudio playerAudio;
    
    // --- Internal Variables ---
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalVelocity = Vector3.zero;
    private bool isGrounded;
    private Transform mainCameraTransform;
    private float environmentSpeedMultiplier = 1f; 
    private Coroutine slowCoroutine;
    
    // State Flags
    private bool isDead;
    private bool isInteractionLocked; 
    private bool isClimbing;
    private bool isEnteringLadder;

    // Ladder Reference
    private Ladder nearbyLadder;
    private float _climbCooldownTimer=0f;

    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public float CurrentHorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsDead => isDead; 
    public bool IsInteractionLocked => isInteractionLocked;
    public bool IsClimbing => isClimbing;
    public bool IsEnteringLadder => isEnteringLadder;
    public bool IsGrounded => isGrounded;
    public bool IsSlowWalking => InputManager.Instance.IsSlowWalking;
    public bool IsSprinting => InputManager.Instance.IsSprinting;
    
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
        if (isClimbing || isEnteringLadder || isInteractionLocked || isDead) return;

        if (isGrounded)
        { 
            jumpRequest = true;
            traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
        }
    }

    void Update()
    {
        if (isDead || isInteractionLocked || isEnteringLadder)
        {
            if (isDead || isInteractionLocked) { ApplyGravityAndFall(); }
            return; 
        }

        if (!isClimbing && nearbyLadder != null && Time.time > _climbCooldownTimer)
        {
            if (InputManager.Instance.MoveInput.y > 0.1f)
            {
                StartClimbing();
                return;
            }
        }

        if (isClimbing)
        {
            HandleClimbing();
            return;
        }

        isGrounded = groundCheck();
        ReportSprintStatus();

        if (isGrounded && !wasGrounded)
        {
            float fallIntensity = Mathf.Abs(velocity.y);
            particleController?.PlayLandEffect(fallIntensity);
            particleController?.ToggleTrail(isGrounded, IsSlowWalking);
            playerAnimation?.Land();
            playerAudio?.PlayLand(fallIntensity);
        }
        wasGrounded = isGrounded;

        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }

        HandleMovement();
    }
    private void ReportSprintStatus()
    {
        bool isPhysicallySprinting = IsSprinting && CurrentHorizontalSpeed > 0.1f && !IsClimbing;
        if (isSprinting != null)
        {
            isSprinting.Value = isPhysicallySprinting;
        }
    }
    private void ApplyGravityAndFall()
    {
        isGrounded = groundCheck();
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        ApplyGravity();
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    // --- CLIMBING LOGIC ---
    private void StartClimbing()
    {
        if (isEnteringLadder || nearbyLadder == null) return;
        StartCoroutine(EnterLadderRoutine());
    }

    private IEnumerator EnterLadderRoutine()
    {
        isEnteringLadder = true;
        isClimbing = false;
        velocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;

        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 ladderPos = nearbyLadder.transform.position;
        Vector3 targetPos = new Vector3(ladderPos.x, transform.position.y, ladderPos.z);
        targetPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.1f);
        Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);

        while (elapsedTime < ladderEntryDuration)
        {
            if (nearbyLadder == null) { isEnteringLadder = false; yield break; }
            float t = elapsedTime / ladderEntryDuration;
            controller.enabled = false;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            controller.enabled = true;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isEnteringLadder = false;
        isClimbing = true;
    }

   private void StopClimbing()
    {
        isClimbing = false;
        isEnteringLadder = false;
        _climbCooldownTimer = Time.time + 0.3f;
    }

    private void HandleClimbing()
    {
        if (nearbyLadder == null) { StopClimbing(); return; }

        float feetY = transform.position.y - (controller.height * 0.5f);
        float stopYPosition = nearbyLadder.GetLadderTopY() - climbTopOffset;
        bool isAtTopPerch = (feetY >= stopYPosition);

        if (isAtTopPerch && InputManager.Instance.IsJumpHeld)
        {
            StopClimbing();
            velocity.y = topDismountUpwardForce;
            horizontalVelocity = -transform.forward * topDismountBackwardForce;
            return;
        }

        float verticalInput = InputManager.Instance.MoveInput.y;

        if (isAtTopPerch && verticalInput > 0)
        {
            verticalInput = 0;
        }

        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            controller.Move(Vector3.up * verticalInput * climbSpeed * Time.deltaTime);
        }

        if (verticalInput < 0 && (controller.collisionFlags & CollisionFlags.Below) != 0)
        {
            StopClimbing();
        }
    }

    // --- NORMAL MOVEMENT LOGIC ---
    private void HandleMovement()
    {
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

        if (jumpRequest && isGrounded)
        {
            particleController?.PlayJumpEffect();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerAnimation?.Jump();
            playerAudio?.PlayJump();
            jumpRequest = false; 
        }

        ApplyGravity();
        
        float currentMoveSpeed = moveSpeed;
        bool hasEnergy = currentEnergy.Value > 0;

        if (hasEnergy)
        {
            // Normal behavior: Player can sprint, walk, or slow walk.
            if (IsSprinting) 
            {
                currentMoveSpeed *= sprintSpeedMultiplier;
            }
            else if (IsSlowWalking) 
            {
                currentMoveSpeed *= slowWalkSpeedMultiplier;
            }
        }
        else
        {
            // NO ENERGY: Force player into slow walk speed.
            currentMoveSpeed *= slowWalkSpeedMultiplier;
        }
        // ------------------------------------------
        
        currentMoveSpeed *= environmentSpeedMultiplier; 

        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * currentMoveSpeed;
        float currentAcceleration = (WorldSpaceMoveDirection.magnitude > 0.1f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, currentAcceleration * Time.deltaTime);
        
        if (horizontalVelocity.magnitude < 0.01f) 
        {
            horizontalVelocity = Vector3.zero;
        }
        
        Vector3 totalVelocity = horizontalVelocity + (Vector3.up * velocity.y);
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