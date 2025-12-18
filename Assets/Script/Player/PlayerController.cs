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
    [Tooltip("How much faster light drains when sprinting (e.g. 2x faster).")]
    [SerializeField] private float sprintEnergyDrainMult = 2.0f; 

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
    [SerializeField] private WispMapLightController wispController;
    
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
    private bool isLaunchingFromLadder; 

    // Ladder Reference
    private Ladder nearbyLadder;
    private float _climbCooldownTimer=0f;

    public Vector3 WorldSpaceMoveDirection { get; private set; }
    public bool IsDead => isDead; 
    public bool IsInteractionLocked => isInteractionLocked;
    public bool IsClimbing => isClimbing;
    public bool IsEnteringLadder => isEnteringLadder;

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
        // FIX: If we were climbing when we left the trigger, force us to stop.
        if (isClimbing)
        {
            StopClimbing();
        }
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
        // THE FIX: If we are climbing, this is a dismount, not a jump. Ignore the global handler.
        if (isClimbing || isEnteringLadder || isInteractionLocked || isDead) return;

        // This code will now ONLY run if we are on the ground and not on a ladder.
        if (isGrounded)
        { 
            jumpRequest = true;
            TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseMedium);
        }
    }

    void Update()
    {
        // --- HIGHEST PRIORITY: Dead/Locked/Transitioning ---
        if (isDead || isInteractionLocked || isEnteringLadder)
        {
            if (isDead || isInteractionLocked) { ApplyGravityAndFall(); }
            return; // Halt all other logic
        }

        // --- STATE CHANGE: Start Climbing ---
        if (!isClimbing && nearbyLadder != null && Time.time > _climbCooldownTimer)
        {
            if (InputManager.Instance.MoveInput.y > 0.1f)
            {
                StartClimbing();
                return;
            }
        }

        // --- ACTIVE STATE: Is Climbing ---
        if (isClimbing)
        {
            HandleClimbing();
            return;
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
        HandleEnergyDrain();
        wasGrounded = isGrounded;

        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }

        HandleMovement();
    }


    private void HandleEnergyDrain()
    {
        if (LightEnergyManager.Instance == null) return;

        // Condition: Is Sprinting AND actually moving AND not Climbing
        bool isSprintingAndMoving = IsSprinting && horizontalVelocity.magnitude > 0.1f && !isClimbing;

        if (isSprintingAndMoving)
        {
            LightEnergyManager.Instance.SetDrainMultiplier(sprintEnergyDrainMult);
        }
        else
        {
            // Reset to normal speed
            LightEnergyManager.Instance.SetDrainMultiplier(1.0f);
        }
    }

    // --- CLIMBING LOGIC ---
    private void ApplyGravityAndFall()
    {
        isGrounded = groundCheck();
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        ApplyGravity();
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }


    // --- CLIMBING LOGIC (DEFINITIVE VERSION) ---
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

        // --- Calculate player's position relative to the ladder top ---
        float feetY = transform.position.y - (controller.height * 0.5f);
        float stopYPosition = nearbyLadder.GetLadderTopY() - climbTopOffset;
        bool isAtTopPerch = (feetY >= stopYPosition);


        // --- 1. HANDLE JUMP EXIT (MODIFIED) ---
        // Player can now ONLY jump if they are at the top perch.
        if (isAtTopPerch && InputManager.Instance.IsJumpHeld)
        {
            StopClimbing();

            // Perform the powerful dismount launch
            velocity.y = topDismountUpwardForce;
            horizontalVelocity = -transform.forward * topDismountBackwardForce;
            
            return; // Exit. Physics will apply next frame.
        }


        // --- 2. MOVEMENT LOGIC (WITH INVISIBLE CEILING) ---
        float verticalInput = InputManager.Instance.MoveInput.y;

        // If at the top perch and trying to move up, clamp input to zero.
        if (isAtTopPerch && verticalInput > 0)
        {
            verticalInput = 0;
        }

        // Only move if there's input, preventing the slow slide.
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            controller.Move(Vector3.up * verticalInput * climbSpeed * Time.deltaTime);
        }

        // --- 3. HANDLE BOTTOM EXIT ---
        if (verticalInput < 0 && (controller.collisionFlags & CollisionFlags.Below) != 0)
        {
            StopClimbing();
        }
    }
    // --- NORMAL MOVEMENT LOGIC ---
    private void HandleMovement()
    {
        // --- 1. GET INPUT AND CALCULATE WORLD DIRECTION ---
        Vector2 moveInput = InputManager.Instance.MoveInput;

        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; 
        camRight.y = 0;
        camForward.Normalize(); 
        camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        // --- 2. ROTATE PLAYER ---
        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- 3. HANDLE JUMPING ---
        if (jumpRequest && isGrounded)
        {
            particleController?.PlayJumpEffect();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerAnimation?.Jump();
            jumpRequest = false; 
        }

        // --- 4. APPLY GRAVITY ---
        ApplyGravity();
        
        // --- 5. DETERMINE MOVEMENT SPEED (WITH LIGHT CHECK) ---
        float currentMoveSpeed = moveSpeed;
        bool hasLight = (wispController != null && wispController.IsLightActive);

        if (hasLight)
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
            // NO LIGHT: Force player into slow walk speed.
            // Ignore Sprint and normal Walk input.
            currentMoveSpeed *= slowWalkSpeedMultiplier;
        }
        
        // Apply any environmental effects (e.g., branch traps)
        currentMoveSpeed *= environmentSpeedMultiplier; 

        // --- 6. CALCULATE FINAL VELOCITY (with acceleration/deceleration) ---
        Vector3 targetHorizontalVelocity = WorldSpaceMoveDirection * currentMoveSpeed;
        float currentAcceleration = (WorldSpaceMoveDirection.magnitude > 0.1f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, currentAcceleration * Time.deltaTime);
        
        // Clamp small values to zero to prevent sliding
        if (horizontalVelocity.magnitude < 0.01f) 
        {
            horizontalVelocity = Vector3.zero;
        }
        
        // --- 7. MOVE THE CHARACTER CONTROLLER ---
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
        // If we are already being slowed, stop the previous timer so we don't overlap
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        
        slowCoroutine = StartCoroutine(SlowRoutine(slowFactor, duration));
    }

    private IEnumerator SlowRoutine(float factor, float duration)
    {
        environmentSpeedMultiplier = factor;
        
        // Wait for the duration
        yield return new WaitForSeconds(duration);
        
        // Reset speed
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