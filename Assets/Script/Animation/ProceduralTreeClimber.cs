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

    [Header("Turn Logic")]
    [SerializeField] private float sharpTurnThreshold = 75f;
    [SerializeField] private float turnPauseTime = 0.5f;
    [SerializeField] private float turnCooldown = 1.5f;
    [SerializeField] private float turnCrawlSpeed = 0.15f; 

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
    
    // NO MAGIC NUMBERS: Physics Smoothing Settings
    [SerializeField] private float surgeDecaySpeed = 3.0f;
    [SerializeField] private float bobSmoothingSpeed = 5.0f;
    [SerializeField] private float bodyRotationSmoothSpeed = 6.0f;

    [Header("Player Grabbing")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float playerGrabHeight = 1.5f; 
    [SerializeField] private bool singleHandOnly = true;
    [SerializeField] private float grabGracePeriod = 1.0f;

    [Header("Hand Settings")]
    [SerializeField] private float handSpeed = 10f;
    [SerializeField] private float swingArcHeight = 1.2f;
    [SerializeField] private float swingOutward = 0.7f;
    [SerializeField] private float gripSpeed = 10f; // Speed for finger animation

    [Header("Triggers")]
    [SerializeField] private float releaseThreshold = 0.1f;
    [SerializeField] private float maxArmAngle = 100f;
    [SerializeField] private float crossBodyThreshold = -0.2f;

    [Header("Vision")]
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private float minReachDistance = 2.5f;
    [SerializeField] private float maxReachDistance = 8f;
    [Range(0, 180)] [SerializeField] private float viewAngle = 140f;

    [Header("Rotation smoothing")]
    public float minRotationSpeed = 2.0f; 
    public float maxRotationSpeed = 8.0f; 

    // --- State ---
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
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (movementController == null) movementController = GetComponent<MonsterMovement>();
        if (brain == null) brain = GetComponent<MonsterBrain>(); 
        
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

        // --- Turn Pause Logic ---
        CheckSharpTurn();
        if (currentTurnTimer > 0)
        {
            currentTurnTimer -= Time.deltaTime;
            
            // Allow crawling while turning
            movementController.AnimationSpeedFactor = turnCrawlSpeed;
            
            // Kill surge so we don't slide sideways
            currentSurge = Mathf.Lerp(currentSurge, 0f, Time.deltaTime * 10f);
            
            UpdateBodyPhysics();
            UpdatePlayerHold();
            UpdateIKTargets();
            return; 
        }
        
        // --- Standard Move Logic ---
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
        bool allowedToGrabPlayer = false;

        if (brain.IsAttacking) 
        {
            allowedToGrabPlayer = true;
        }
        else if (Time.time - brain.LastTimeSeenPlayer < grabGracePeriod)
        {
            if (brain.LastTimeSeenPlayer > 0) allowedToGrabPlayer = true;
        }

        bool canGrabRight = false;
        bool canGrabLeft = false;

        if (allowedToGrabPlayer)
        {
            canGrabRight = heldPlayerRight == null && ScanForPlayer(true) != null;
            canGrabLeft = heldPlayerLeft == null && ScanForPlayer(false) != null;
        }

        if (singleHandOnly && (heldPlayerRight != null || heldPlayerLeft != null))
        {
            canGrabRight = false;
            canGrabLeft = false;
        }

        if (canGrabRight) { StartCoroutine(SwingHand(true, null, ScanForPlayer(true))); return; }
        if (canGrabLeft) { StartCoroutine(SwingHand(false, null, ScanForPlayer(false))); return; }

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

    Collider FindBestTree(bool isRightHand) {
        Vector3 searchCenter = transform.position + stableForward * (maxReachDistance * 0.6f);
        Collider[] hits = Physics.OverlapSphere(searchCenter, maxReachDistance, treeLayer);
        if (hits.Length == 0) return null;
        Collider bestTree = null;
        float bestScore = float.MinValue;
        Vector3 pathRight = Vector3.Cross(Vector3.up, stableForward);
        float halfViewAngle = viewAngle * 0.5f;
        foreach (var tree in hits) {
            if (tree == leftTreeCollider || tree == rightTreeCollider) continue;
            Vector3 toTree = tree.transform.position - transform.position;
            float dist = toTree.magnitude;
            if (dist < minReachDistance) continue;
            Vector3 dirToTree = toTree / dist; 
            float sideDot = Vector3.Dot(toTree, pathRight);
            if ((isRightHand && sideDot < -LaneOverlap) || (!isRightHand && sideDot > LaneOverlap)) continue;
            if (Vector3.Angle(stableForward, dirToTree) > halfViewAngle) continue;
            float pathAlign = Vector3.Dot(stableForward, dirToTree);
            float score = pathAlign * PathAlignWeight + dist * DistanceWeight;
            if (score > bestScore) { bestScore = score; bestTree = tree; }
        }
        return bestTree;
    }

    IEnumerator SwingHand(bool isRight, Collider targetTree, Transform targetPlayer) {
        isHandMoving = true;
        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;
        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;
        if (targetPlayer != null) {
            finalPos = targetPlayer.position + Vector3.up * playerGrabHeight;
            finalRot = Quaternion.LookRotation((finalPos - transform.position).normalized, Vector3.up);
        } else {
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
        while (elapsed <= totalDist) {
            float dynamicSpeed = Mathf.Max(handSpeed, agent.velocity.magnitude * 3f);
            elapsed += Time.deltaTime * dynamicSpeed;
            float t = Mathf.Clamp01(elapsed / totalDist);
            float smoothT = t * t * (3f - 2f * t); 
            movementController.AnimationSpeedFactor = Mathf.Lerp(movementController.AnimationSpeedFactor, reachSpeedFactor, Time.deltaTime * 10f);
            float gripVal = 0f;
            if (t < 0.2f) gripVal = 1f - (t / 0.2f);
            else if (t > 0.8f) gripVal = (t - 0.8f) / 0.2f;
            animator.SetFloat(currentGripHash, gripVal);
            if (targetPlayer != null) finalPos = targetPlayer.position + Vector3.up * playerGrabHeight;
            float u = 1f - smoothT;
            Vector3 pos = u * u * startPos + 2f * u * smoothT * controlPoint + smoothT * smoothT * finalPos;
            if (isRight) { rightHandPos = pos; rightHandRot = Quaternion.Slerp(startRot, finalRot, smoothT); }
            else         { leftHandPos = pos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, smoothT); }
            if (t >= 1.0f) break;
            yield return null;
        }
        animator.SetFloat(currentGripHash, 1f);
        if (isRight) {
            if (targetPlayer != null) { heldPlayerRight = targetPlayer; rightTreeCollider = null; }
            else { rightTreeCollider = targetTree; rightHandPos = finalPos; rightHandRot = finalRot; }
        } else {
            if (targetPlayer != null) { heldPlayerLeft = targetPlayer; leftTreeCollider = null; }
            else { leftTreeCollider = targetTree; leftHandPos = finalPos; leftHandRot = finalRot; }
        }
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
        Vector3[] corners = agent.path.corners;
        if (corners.Length < 2) return transform.position + transform.forward * lookAheadDist;
        Vector3 currentPos = transform.position;
        float distCovered = 0f;
        for (int i = 0; i < corners.Length - 1; i++) {
            Vector3 segStart = (i == 0) ? currentPos : corners[i];
            Vector3 segEnd = corners[i + 1];
            float segDist = Vector3.Distance(segStart, segEnd);
            if (distCovered + segDist >= lookAheadDist) return (Vector3.MoveTowards(segStart, segEnd, lookAheadDist - distCovered) - transform.position).normalized;
            distCovered += segDist;
        }
        return corners[corners.Length - 1] - transform.position;
    }

    bool IsHandStressed(bool isRight)
    {
        Vector3 handPos = isRight ? rightHandPos : leftHandPos;
        Vector3 toHand = handPos - transform.position;
        if (Vector3.Angle(stableForward, toHand) > maxArmAngle) return true;
        Vector3 stableRight = Vector3.Cross(Vector3.up, stableForward);
        float sideDist = Vector3.Dot(toHand, stableRight);
        if (isRight && sideDist < crossBodyThreshold) return true;
        if (!isRight && sideDist > -crossBodyThreshold) return true;
        float depth = Vector3.Dot(toHand, stableForward);
        return depth < releaseThreshold;
    }

    void UpdateBodyPhysics()
    {
        if (visualModel == null) return;

        // --- 1. POSITION ---
        Vector3 handCenter = (leftHandPos + rightHandPos) / 2f;
        Vector3 targetWorldPos = handCenter;

        // Lag
        if (agent.velocity.magnitude > 0.1f) 
            targetWorldPos -= agent.velocity.normalized * bodyLag;
        
        // Surge (Decay using variable)
        currentSurge = Mathf.Lerp(currentSurge, 0f, Time.deltaTime * surgeDecaySpeed);
        targetWorldPos += transform.forward * currentSurge;

        // --- FIX: SMOOTH BOBBING ---
        float speedFactor = movementController.AnimationSpeedFactor;
        float liftProgress = Mathf.InverseLerp(reachSpeedFactor, pullSpeedFactor, speedFactor);
        
        // Calculate TARGET bob
        float targetBob = Mathf.Lerp(-dropAmount, liftAmount, liftProgress);
        
        // Smoothly interpolate currentBobY (Variable)
        currentBobY = Mathf.Lerp(currentBobY, targetBob, Time.deltaTime * bobSmoothingSpeed);
        
        targetWorldPos.y += currentBobY;

        // Apply Position
        Vector3 targetLocalPos = transform.InverseTransformPoint(targetWorldPos);
        targetLocalPos = Vector3.ClampMagnitude(targetLocalPos, 1.0f);
        visualModel.localPosition = Vector3.SmoothDamp(
            visualModel.localPosition, 
            targetLocalPos, 
            ref bodyVelocity, 
            bodySmoothTime
        );

        // --- 2. ROTATION ---
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float targetPitch = Mathf.Lerp(5f, maxForwardLean, speedRatio);

        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);
        
        // Hysteresis for Jiggle Fix
        if (localRight.z < localLeft.z - 0.2f) isLeaningRight = true;
        else if (localLeft.z < localRight.z - 0.2f) isLeaningRight = false;

        targetYaw = isLeaningRight ? bodyTwistAmount : -bodyTwistAmount;
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * 3f);

        Vector3 moveDir = agent.velocity;
        if (moveDir.magnitude < 0.1f) moveDir = transform.forward;
        
        // Dynamic Rotation Speed based on angle
        float angleDiff = Vector3.Angle(visualModel.forward, moveDir);
        float rotationSpeed = Mathf.Lerp(maxRotationSpeed, minRotationSpeed, angleDiff / 90f);

        Quaternion lookRot = Quaternion.LookRotation(moveDir, Vector3.up);
        Quaternion offsetRot = Quaternion.Euler(targetPitch, currentYaw, 0);

        visualModel.rotation = Quaternion.Slerp(visualModel.rotation, lookRot * offsetRot, Time.deltaTime * rotationSpeed);
    }

    void UpdateIKTargets()
    {
        // FIX: Hard Set the position. Do NOT use Lerp here.
        // This stops the jiggle because the target is locked perfectly to the tree.
        if (leftHandTarget != null) 
        {
            leftHandTarget.position = leftHandPos;
            leftHandTarget.rotation = leftHandRot;
        }
        if (rightHandTarget != null)
        {
            rightHandTarget.position = rightHandPos;
            rightHandTarget.rotation = rightHandRot;
        }
    }
}