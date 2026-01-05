using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using CrashKonijn.Goap.MonsterGen.Capabilities;

public class ProceduralMonsterController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform visualModel;
    [SerializeField] private Animator animator;
    [SerializeField] private MonsterMovement movementController;
    [SerializeField] private MonsterBrain brain;

    [Header("IK Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Path Smoothing")]
    [SerializeField] private float lookAheadDist = 5f;
    [SerializeField] private float logicTurnSpeed = 4f;

    [Header("Turn Logic (The Fix)")]
    [Tooltip("Angle to trigger a sharp turn state.")]
    [SerializeField] private float sharpTurnThreshold = 65f; // Lowered slightly to catch turns earlier
    [Tooltip("Stricter angle check specifically during turns.")]
    [SerializeField] private float turnStressAngle = 60f; 
    [SerializeField] private float turnPauseTime = 0.5f;
    [SerializeField] private float turnCooldown = 1.0f;
    [SerializeField] private float turnCrawlSpeed = 0.1f; 

    [Header("Rhythm & Speed")]
    [SerializeField] private float reachSpeedFactor = 0.3f; 
    [SerializeField] private float pullSpeedFactor = 2.2f; 
    [SerializeField] private float speedDecay = 2.0f;

    [Header("Visual Physics")]
    [SerializeField] private float dropAmount = 0.6f;
    [SerializeField] private float liftAmount = 0.2f;
    [SerializeField] private float bodyLag = 0.4f;
    [SerializeField] private float bodySmoothTime = 0.15f;
    [SerializeField] private float maxForwardLean = 45f;
    [SerializeField] private float bodyTwistAmount = 15f;
    
    [SerializeField] private float surgeDecaySpeed = 3.0f;
    [SerializeField] private float bobSmoothingSpeed = 5.0f;

    [Header("Player Grabbing")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float playerGrabHeight = 1.5f; 
    [SerializeField] private bool singleHandOnly = true;
    [SerializeField] private float grabGracePeriod = 1.0f;

    [Header("Hand Settings")]
    [SerializeField] private float handSpeed = 10f;
    [SerializeField] private float swingArcHeight = 1.2f;
    [SerializeField] private float swingOutward = 0.7f;

    [Header("Triggers")]
    [SerializeField] private float releaseThreshold = 0.1f;
    [SerializeField] private float maxArmAngle = 100f; // Normal walking angle limit
    [SerializeField] private float crossBodyThreshold = -0.2f;

    [Header("Vision")]
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private float minReachDistance = 2.5f;
    [SerializeField] private float maxReachDistance = 8f;
    [Range(0, 180)] [SerializeField] private float viewAngle = 140f;

    [Header("Rotation smoothing")]
    public float minRotationSpeed = 2.0f; 
    public float maxRotationSpeed = 8.0f; 
    [Header("Audio")]
    [SerializeField] private SoundDefinition sfx_TreeGrab;
    [SerializeField] private SoundDefinition sfx_PlayerGrab;

    //State
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider;
    private bool isHandMoving;
    private Vector3 stableForward;
    
    // Player State
    private Transform heldPlayerRight = null;
    private Transform heldPlayerLeft = null;

    // Physics State
    private Vector3 bodyVelocity; 
    private int leftGripHash, rightGripHash;
    private float currentSurge = 0f;
    private float currentBobY = 0f;

    // Jiggle Fixes
    private float currentYaw = 0f; 
    private float targetYaw = 0f; 
    private bool isLeaningRight = true; 

    // Turn Logic State
    private float currentTurnTimer = 0f;
    private float lastTurnTimestamp = 0f;

    // Constants
    private const float MinVelocity = 0.1f;
    private const float LaneOverlap = 0.3f;
    private const float PathAlignWeight = 8f;
    private const float DistanceWeight = 2f;

    void Start()
    {   
        leftGripHash = Animator.StringToHash("LeftGrip");
        rightGripHash = Animator.StringToHash("RightGrip");

        leftHandPos = leftHandTarget.position;
        leftHandRot = leftHandTarget.rotation;
        rightHandPos = rightHandTarget.position;
        rightHandRot = rightHandTarget.rotation;
        
        stableForward = transform.forward;
        
        DetectInitialTree(leftHandPos, ref leftTreeCollider);
        DetectInitialTree(rightHandPos, ref rightTreeCollider);

        animator.SetFloat(leftGripHash, 1f);
        animator.SetFloat(rightGripHash, 1f);
    }

    void DetectInitialTree(Vector3 pos, ref Collider treeRef)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 1f, treeLayer);
        if (hits.Length > 0) treeRef = hits[0];
    }

    void LateUpdate()
    {
        if (this == null || gameObject == null || agent == null) return;

        UpdateStableForward();

        CheckSharpTurn();
        if (currentTurnTimer > 0)
        {
            currentTurnTimer -= Time.deltaTime;
            
            movementController.AnimationSpeedFactor = turnCrawlSpeed;
            currentSurge = Mathf.Lerp(currentSurge, 0f, Time.deltaTime * 10f);
            
            if (!isHandMoving)
            {
                ForceHandSwitchDuringTurn();
            }

            UpdateBodyPhysics();
            UpdatePlayerHold();
            UpdateIKTargets();
            return; 
        }
        
        if (!isHandMoving && agent.velocity.sqrMagnitude > MinVelocity * MinVelocity)
        {
            CheckAndMoveHands();
        }

        UpdatePlayerHold();
        UpdateSpeedDecay();
        UpdateBodyPhysics();
        UpdateIKTargets();
    }

    void CheckSharpTurn()
    {
        if (currentTurnTimer > 0) return;
        if (Time.time < lastTurnTimestamp + turnCooldown) return;
        if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.5f) return;

        Vector3 toSteering = (agent.steeringTarget - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toSteering);

        if (angle > sharpTurnThreshold)
        {
            currentTurnTimer = turnPauseTime;
            lastTurnTimestamp = Time.time;
        }
    }

    void ForceHandSwitchDuringTurn()
    {
        Vector3 toSteering = (agent.steeringTarget - transform.position).normalized;
        Vector3 cross = Vector3.Cross(transform.forward, toSteering);
        bool turningRight = cross.y > 0;

        bool targetIsRight = !turningRight; 

        Vector3 handPos = targetIsRight ? rightHandPos : leftHandPos;
        Vector3 toHand = handPos - transform.position;
        float angle = Vector3.Angle(transform.forward, toHand);

        if (angle > turnStressAngle)
        {
            AttemptStep(targetIsRight);
        }
    }

    void UpdateSpeedDecay()
    {
        movementController.AnimationSpeedFactor = Mathf.MoveTowards(
            movementController.AnimationSpeedFactor, 
            1.0f, 
            Time.deltaTime * speedDecay
        );
    }

    void UpdatePlayerHold()
    {
        if (heldPlayerRight != null)
        {
            rightHandPos = heldPlayerRight.position + Vector3.up * playerGrabHeight;
            rightHandRot = Quaternion.LookRotation(heldPlayerRight.forward, Vector3.up);
        }
        if (heldPlayerLeft != null)
        {
            leftHandPos = heldPlayerLeft.position + Vector3.up * playerGrabHeight;
            leftHandRot = Quaternion.LookRotation(heldPlayerLeft.forward, Vector3.up);
        }
    }

    void CheckAndMoveHands()
    {
        // --- 1. LOGIC GATE: ARE WE ALLOWED TO GRAB? ---
        bool allowedToGrabPlayer = false;

        if (brain.IsAttacking) 
        {
            allowedToGrabPlayer = true;
        }
        else if (Time.time - brain.LastTimeSeenPlayer < grabGracePeriod)
        {
            if (brain.LastTimeSeenPlayer > 0) allowedToGrabPlayer = true;
        }

        // STATE LEAK FIX: If not allowed, but still holding, force release.
        if (!allowedToGrabPlayer)
        {
            if (heldPlayerRight != null) heldPlayerRight = null;
            if (heldPlayerLeft != null) heldPlayerLeft = null;
        }

        // --- 2. PLAYER GRAB ATTEMPT ---
        bool canGrabRight = false;
        bool canGrabLeft = false;

        // Only scan for player if the logic gate passed
        if (allowedToGrabPlayer)
        {
            canGrabRight = heldPlayerRight == null && ScanForPlayer(true) != null;
            canGrabLeft = heldPlayerLeft == null && ScanForPlayer(false) != null;
        }

        // (Code from here is new/updated)
        
        // Don't grab if a single hand is already holding (if configured)
        if (singleHandOnly && (heldPlayerRight != null || heldPlayerLeft != null))
        {
            canGrabRight = false;
            canGrabLeft = false;
        }
        
        // --- TURN ORDER FIX: ENFORCE ALTERNATING GRABS ---
        // If both hands are valid targets, only allow the one whose turn it is.
        if (canGrabRight && canGrabLeft)
        {
            if (isLeaningRight) // Assuming isLeaningRight tracks the "next" hand to move
                canGrabLeft = false;
            else
                canGrabRight = false;
        }

        // Execute Player Grab if possible
        if (canGrabRight) 
        {
            StartCoroutine(SwingHand(true, null, ScanForPlayer(true)));
            isLeaningRight = !isLeaningRight; // Swap turn
            return; 
        }
        if (canGrabLeft) 
        {
            StartCoroutine(SwingHand(false, null, ScanForPlayer(false)));
            isLeaningRight = !isLeaningRight; // Swap turn
            return;
        }

        // --- 3. STANDARD TREE CLIMBING (If player grab failed/invalid) ---
        // Don't move a hand if it's currently holding a player
        bool rightStressed = (heldPlayerRight == null) && IsHandStressed(true);
        bool leftStressed = (heldPlayerLeft == null) && IsHandStressed(false);

        if (rightStressed && leftStressed)
        {
            float rightDepth = Vector3.Dot(rightHandPos - transform.position, stableForward);
            float leftDepth = Vector3.Dot(leftHandPos - transform.position, stableForward);
            AttemptStep(rightDepth < leftDepth);
        }
        else if (rightStressed) AttemptStep(true);
        else if (leftStressed) AttemptStep(false);
    }

    Transform ScanForPlayer(bool isRightHand)
    {
        Vector3 searchCenter = transform.position + stableForward * (maxReachDistance * 0.5f);
        Collider[] hits = Physics.OverlapSphere(searchCenter, maxReachDistance, playerLayer);
        foreach(var hit in hits) {
            Vector3 toTarget = hit.transform.position - transform.position;
            Vector3 dir = toTarget.normalized;
            Vector3 pathRight = Vector3.Cross(Vector3.up, stableForward);
            float sideDot = Vector3.Dot(toTarget, pathRight);
            if (isRightHand && sideDot < -0.1f) continue;
            if (!isRightHand && sideDot > 0.1f) continue;
            if (Vector3.Angle(stableForward, dir) > viewAngle / 2f) continue;
            return hit.transform;
        }
        return null;
    }

    bool AttemptStep(bool isRightHand) {
        Collider targetTree = FindBestTree(isRightHand);
        if (targetTree != null) { StartCoroutine(SwingHand(isRightHand, targetTree, null)); return true; }
        return false;
    }

    Collider FindBestTree(bool isRightHand)
    {
        // 1. Setup Search Area
        Vector3 searchCenter = transform.position + (stableForward * (maxReachDistance * 0.6f));
        Collider[] hits = Physics.OverlapSphere(searchCenter, maxReachDistance, treeLayer);

        if (hits.Length == 0) return null;

        // 2. Pre-calculate Comparison Vectors
        Vector3 pathRight = Vector3.Cross(Vector3.up, stableForward);
        
        // Optimization: Convert Angle to Dot Product Threshold (Cheaper than calculating Angle inside loop)
        // 1.0 = 0 degrees, 0.0 = 90 degrees. 
        float minViewDot = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);

        Collider bestTree = null;
        float bestScore = float.MinValue;

        foreach (var tree in hits)
        {
            // --- VALIDITY CHECKS ---

            // A. Ignore trees we are already holding
            if (tree == leftTreeCollider || tree == rightTreeCollider) continue;

            Vector3 vectorToTree = tree.transform.position - transform.position;
            float distSqr = vectorToTree.sqrMagnitude; // Faster distance check

            // B. Min Reach Check (Squared comparison is faster)
            if (distSqr < minReachDistance * minReachDistance) continue;

            // Normalize for direction checks
            Vector3 directionToTree = vectorToTree.normalized;

            // C. Lane Check (Left vs Right side)
            float horizontalAlignment = Vector3.Dot(vectorToTree, pathRight);
            
            // If Right Hand: Allow anything on Right (> 0) and slightly Left (> -LaneOverlap)
            if (isRightHand && horizontalAlignment < -LaneOverlap) continue;
            
            // If Left Hand: Allow anything on Left (< 0) and slightly Right (< LaneOverlap)
            if (!isRightHand && horizontalAlignment > LaneOverlap) continue;

            // D. Vision Cone Check
            float forwardAlignment = Vector3.Dot(stableForward, directionToTree);
            if (forwardAlignment < minViewDot) continue;

            // --- SCORING ---
            
            // We want trees that are:
            // 1. Perfectly aligned with our forward path (High Forward Alignment)
            // 2. Far away (High Distance) -> Encourages long, heavy strides
            
            float distance = Mathf.Sqrt(distSqr);
            float score = (forwardAlignment * PathAlignWeight) + (distance * DistanceWeight);

            if (score > bestScore)
            {
                bestScore = score;
                bestTree = tree;
            }
        }

        return bestTree;
    }

    IEnumerator SwingHand(bool isRight, Collider targetTree, Transform targetPlayer)
    {
        isHandMoving = true;

        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;

        // --- SETUP TARGET ---
        if (targetPlayer != null)
        {
            finalPos = targetPlayer.position + Vector3.up * playerGrabHeight;
            finalRot = Quaternion.LookRotation((finalPos - transform.position).normalized, Vector3.up);
        }
        else
        {
            Vector3 targetCenter = targetTree.bounds.center;
            Vector3 dirToTree = (targetCenter - transform.position).normalized;
            finalPos = targetCenter;
            Vector3 surfaceNormal = -dirToTree;
            Ray ray = new Ray(transform.position + Vector3.up * 1.5f, dirToTree);
            if (targetTree.Raycast(ray, out RaycastHit hit, 20f)) { finalPos = hit.point; surfaceNormal = hit.normal; }
            finalRot = Quaternion.LookRotation(-surfaceNormal, transform.up);
        }

        Vector3 midPoint = (startPos + finalPos) * 0.5f;
        Vector3 sideOffset = (isRight ? transform.right : -transform.right) * swingOutward;
        Vector3 controlPoint = midPoint + Vector3.up * swingArcHeight + sideOffset;

        float totalDist = Vector3.Distance(startPos, finalPos);
        float elapsed = 0f;
        int currentGripHash = isRight ? rightGripHash : leftGripHash;
        
        while (elapsed <= totalDist)
        {
            float dynamicSpeed = Mathf.Max(handSpeed, agent.velocity.magnitude * 3f);
            elapsed += Time.deltaTime * dynamicSpeed;
            float t = Mathf.Clamp01(elapsed / totalDist);
            float smoothT = t * t * (3f - 2f * t);

            // Physics: Slow down agent while reaching
            movementController.AnimationSpeedFactor = Mathf.Lerp(movementController.AnimationSpeedFactor, reachSpeedFactor, Time.deltaTime * 10f);
            
            // Grip Animation
            float gripVal = 0f;
            if (t < 0.2f) gripVal = 1f - (t / 0.2f);
            else if (t > 0.8f) gripVal = (t - 0.8f) / 0.2f;
            animator.SetFloat(currentGripHash, gripVal);

            // --- GRAB FAIL & TRACKING LOGIC ---
            if (targetPlayer != null)
            {
                // Update finalPos to chase the moving player
                finalPos = targetPlayer.position + Vector3.up * playerGrabHeight;

                // If player gets too far away, ABORT.
                // Add +2m buffer to max reach distance
                if (Vector3.Distance(transform.position, finalPos) > maxReachDistance + 2.0f)
                {
                    // Open hand and exit
                    animator.SetFloat(currentGripHash, 0f);
                    isHandMoving = false;
                    yield break; // ABORT GRAB
                }
            }
            
            // Bezier Move
            float u = 1f - smoothT;
            Vector3 pos = u * u * startPos + 2f * u * smoothT * controlPoint + smoothT * smoothT * finalPos;

            if (isRight) { rightHandPos = pos; rightHandRot = Quaternion.Slerp(startRot, finalRot, smoothT); }
            else         { leftHandPos = pos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, smoothT); }

            if (t >= 1.0f) break;
            yield return null;
        }

        // --- FINALIZE & LOCK ---
        animator.SetFloat(currentGripHash, 1f);

        if (isRight)
        {
            if (targetPlayer != null) 
            { 
                heldPlayerRight = targetPlayer; 
                rightTreeCollider = null; 
                
                // Play Player Grab Sound
                if (SoundManager.Instance != null && sfx_PlayerGrab != null)
                    SoundManager.Instance.PlaySound(sfx_PlayerGrab, finalPos);
            }
            else 
            { 
                rightTreeCollider = targetTree; 
                rightHandPos = finalPos; 
                rightHandRot = finalRot; 

                // --- ADDED: Play Tree Grab Sound ---
                if (SoundManager.Instance != null && sfx_TreeGrab != null)
                    SoundManager.Instance.PlaySound(sfx_TreeGrab, finalPos);
            }
        }
        else // Left Hand
        {
            if (targetPlayer != null) 
            { 
                heldPlayerLeft = targetPlayer; 
                leftTreeCollider = null; 

                // Play Player Grab Sound
                if (SoundManager.Instance != null && sfx_PlayerGrab != null)
                    SoundManager.Instance.PlaySound(sfx_PlayerGrab, finalPos);
            }
            else 
            { 
                leftTreeCollider = targetTree; 
                leftHandPos = finalPos; 
                leftHandRot = finalRot; 

                // --- ADDED: Play Tree Grab Sound ---
                if (SoundManager.Instance != null && sfx_TreeGrab != null)
                    SoundManager.Instance.PlaySound(sfx_TreeGrab, finalPos);
            }
        }

        // Physics: Trigger Surge
        movementController.AnimationSpeedFactor = pullSpeedFactor;
        currentSurge = 1.0f;

        isHandMoving = false;
    }

    void UpdateStableForward()
    {
        Vector3 targetDir = agent.hasPath ? GetPathDirection() : transform.forward;
        stableForward = Vector3.Slerp(stableForward, targetDir, Time.deltaTime * logicTurnSpeed).normalized;
    }

    Vector3 GetPathDirection()
    {
        // 1. Safety Checks
        if (agent.path.corners.Length < 2) 
            return transform.position + transform.forward * lookAheadDist;

        // 2. Find the "Rabbit" point on the path
        Vector3 rabbitPoint = GetPointAlongPath(lookAheadDist);
        
        // 3. Return direction to that point
        return (rabbitPoint - transform.position).normalized;
    }

    Vector3 GetPointAlongPath(float targetDistance)
    {
        Vector3[] corners = agent.path.corners;
        Vector3 currentPos = transform.position;
        float accumulatedDist = 0f;

        // Iterate through path segments
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 start = (i == 0) ? currentPos : corners[i];
            Vector3 end = corners[i + 1];
            
            float segmentLength = Vector3.Distance(start, end);

            // If the target point lies within this segment
            if (accumulatedDist + segmentLength >= targetDistance)
            {
                float remainingDist = targetDistance - accumulatedDist;
                return Vector3.MoveTowards(start, end, remainingDist);
            }

            accumulatedDist += segmentLength;
        }

        // Fallback: Return the final destination if path is shorter than lookAheadDist
        return corners[corners.Length - 1];
    }

    bool IsHandStressed(bool isRight)
    {
        Vector3 handPos = isRight ? rightHandPos : leftHandPos;
        Vector3 vectorToHand = handPos - transform.position;

        // 1. ANGLE CHECK (T-Pose Fix)
        // Is the arm twisted too far sideways/backwards?
        if (Vector3.Angle(stableForward, vectorToHand) > maxArmAngle) 
            return true;

        // 2. CROSS-BODY CHECK (Tangle Fix)
        // Is the Right hand crossing over to the Left side (or vice versa)?
        Vector3 stableRight = Vector3.Cross(Vector3.up, stableForward);
        float horizontalDist = Vector3.Dot(vectorToHand, stableRight);
        
        if (isRight && horizontalDist < crossBodyThreshold) return true; // Right hand is on Left side
        if (!isRight && horizontalDist > -crossBodyThreshold) return true; // Left hand is on Right side

        // 3. DEPTH CHECK (Standard Walking)
        // Is the hand dragging too far behind the body?
        float forwardDist = Vector3.Dot(vectorToHand, stableForward);
        
        return forwardDist < releaseThreshold;
    }

    void UpdateBodyPhysics()
    {
        if (visualModel == null) return;

        ApplyPositionPhysics();
        ApplyRotationPhysics();
    }

    void ApplyPositionPhysics()
    {
        // A. Calculate Center Mass (Ideal Position)
        Vector3 handCenter = (leftHandPos + rightHandPos) * 0.5f;
        Vector3 targetWorldPos = handCenter;

        // B. Apply Drag (Move body backwards based on speed)
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            targetWorldPos -= agent.velocity.normalized * bodyLag;
        }

        // C. Apply Surge (The "Pull" force)
        currentSurge = Mathf.Lerp(currentSurge, 0f, Time.deltaTime * surgeDecaySpeed);
        targetWorldPos += transform.forward * currentSurge;

        // D. Apply Bob (Height changes based on reach/pull state)
        float speedFactor = movementController.AnimationSpeedFactor;
        float liftProgress = Mathf.InverseLerp(reachSpeedFactor, pullSpeedFactor, speedFactor);
        
        float targetBob = Mathf.Lerp(-dropAmount, liftAmount, liftProgress);
        currentBobY = Mathf.Lerp(currentBobY, targetBob, Time.deltaTime * bobSmoothingSpeed);
        
        targetWorldPos.y += currentBobY;

        // E. Convert to Local & Clamp (Prevents teleporting)
        Vector3 targetLocalPos = transform.InverseTransformPoint(targetWorldPos);
        targetLocalPos = Vector3.ClampMagnitude(targetLocalPos, 1.0f);

        // F. Apply Smoothly
        visualModel.localPosition = Vector3.SmoothDamp(
            visualModel.localPosition, 
            targetLocalPos, 
            ref bodyVelocity, 
            bodySmoothTime
        );
    }

    void ApplyRotationPhysics()
    {
        Vector3 moveDir = agent.velocity;
        if (moveDir.sqrMagnitude < 0.01f) moveDir = transform.forward;

        // 1. Pitch (Lean Forward)
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float targetPitch = Mathf.Lerp(5f, maxForwardLean, speedRatio);

        // 2. Yaw (Body Twist)
        // Determine which hand is the "Anchor" (furthest back)
        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);
        
        // Hysteresis: Only switch twist if hands pass each other significantly
        // This prevents jitter when hands are side-by-side
        if (localRight.z < localLeft.z - 0.2f) isLeaningRight = true;
        else if (localLeft.z < localRight.z - 0.2f) isLeaningRight = false;

        targetYaw = isLeaningRight ? bodyTwistAmount : -bodyTwistAmount;
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * 3f);

        // 3. Rotation Speed (Slow down rotation on sharp turns)
        float angleDiff = Vector3.Angle(visualModel.forward, moveDir);
        float rotationSpeed = Mathf.Lerp(maxRotationSpeed, minRotationSpeed, angleDiff / 90f);

        // 4. Apply
        Quaternion lookRot = Quaternion.LookRotation(moveDir, Vector3.up);
        Quaternion offsetRot = Quaternion.Euler(targetPitch, currentYaw, 0);

        visualModel.rotation = Quaternion.Slerp(visualModel.rotation, lookRot * offsetRot, Time.deltaTime * rotationSpeed);
    }
    void UpdateIKTargets()
    {
        if (leftHandTarget != null) {
            leftHandTarget.position = leftHandPos;
            leftHandTarget.rotation = leftHandRot;
        }
        if (rightHandTarget != null) {
            rightHandTarget.position = rightHandPos;
            rightHandTarget.rotation = rightHandRot;
        }
    }
}