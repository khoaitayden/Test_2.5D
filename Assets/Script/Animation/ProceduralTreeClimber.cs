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

    [Header("IK Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Path Smoothing")]
    [SerializeField] private float lookAheadDist = 5f;
    [SerializeField] private float logicTurnSpeed = 4f;

    [Header("Turn Logic (The Smooth Fix)")]
    [SerializeField] private float sharpTurnThreshold = 75f;
    [SerializeField] private float turnPauseTime = 0.5f;
    [SerializeField] private float turnCooldown = 1.5f;
    [Tooltip("Speed during a sharp turn (0.1 = Slow Crawl, 0.0 = Stop).")]
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

    [Header("Player Grabbing")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float playerGrabHeight = 1.5f; 
    [SerializeField] private bool singleHandOnly = true;

    [Header("Hand Settings")]
    [SerializeField] private float handSpeed = 10f;
    [SerializeField] private float swingArcHeight = 1.2f;
    [SerializeField] private float swingOutward = 0.7f;

    [Header("Triggers")]
    [SerializeField] private float releaseThreshold = 0.1f;
    [SerializeField] private float maxArmAngle = 100f;
    [SerializeField] private float crossBodyThreshold = -0.2f;

    [Header("Vision")]
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private float minReachDistance = 2.5f;
    [SerializeField] private float maxReachDistance = 8f;
    [Range(0, 180)] [SerializeField] private float viewAngle = 140f;

    // --- State ---
    // We treat these as "Target World Positions"
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

        leftGripHash = Animator.StringToHash("LeftGrip");
        rightGripHash = Animator.StringToHash("RightGrip");

        // Detach logic from hierarchy position to prevent double-transform jitter
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
        // ERROR FIX: Stop execution if object is destroying
        if (this == null || gameObject == null || agent == null) return;

        UpdateStableForward();

        // --- TURN PAUSE LOGIC ---
        CheckSharpTurn();

        if (currentTurnTimer > 0)
        {
            currentTurnTimer -= Time.deltaTime;
            
            // FIX: Don't stop completely. Crawl to allow rotation.
            movementController.AnimationSpeedFactor = turnCrawlSpeed;
            
            currentSurge = Mathf.Lerp(currentSurge, 0f, Time.deltaTime * 10f);

            UpdateBodyPhysics();
            UpdatePlayerHold();
            UpdateIKTargets(); // Keep hands pinned
            return; 
        }
        // ------------------------
        
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

        if (angle > sharpTurnThreshold) // e.g. > 75 degrees
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
        // If holding a player, we must update the position every frame
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
        // 1. Priority: Grab Player
        bool canGrabRight = heldPlayerRight == null && ScanForPlayer(true) != null;
        bool canGrabLeft = heldPlayerLeft == null && ScanForPlayer(false) != null;

        if (singleHandOnly && (heldPlayerRight != null || heldPlayerLeft != null))
        {
            canGrabRight = false;
            canGrabLeft = false;
        }

        if (canGrabRight) { StartCoroutine(SwingHand(true, null, ScanForPlayer(true))); return; }
        if (canGrabLeft) { StartCoroutine(SwingHand(false, null, ScanForPlayer(false))); return; }

        // 2. Standard: Climb Trees
        // Don't move a hand if it's busy holding a player
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

        foreach(var hit in hits)
        {
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

    bool AttemptStep(bool isRightHand)
    {
        Collider targetTree = FindBestTree(isRightHand);
        if (targetTree != null)
        {
            StartCoroutine(SwingHand(isRightHand, targetTree, null));
            return true;
        }
        return false;
    }

    Collider FindBestTree(bool isRightHand)
    {
        Vector3 searchCenter = transform.position + stableForward * (maxReachDistance * 0.6f);
        Collider[] hits = Physics.OverlapSphere(searchCenter, maxReachDistance, treeLayer);
        
        if (hits.Length == 0) return null;

        Collider bestTree = null;
        float bestScore = float.MinValue;
        Vector3 pathRight = Vector3.Cross(Vector3.up, stableForward);
        float halfViewAngle = viewAngle * 0.5f;

        foreach (var tree in hits)
        {
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
            if (targetTree.Raycast(ray, out RaycastHit hit, 20f))
            {
                finalPos = hit.point;
                surfaceNormal = hit.normal;
            }
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

        if (isRight)
        {
            if (targetPlayer != null) { heldPlayerRight = targetPlayer; rightTreeCollider = null; }
            else { rightTreeCollider = targetTree; rightHandPos = finalPos; rightHandRot = finalRot; }
        }
        else
        {
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
        Vector3 handCenter = (leftHandPos + rightHandPos) / 2f;
        Vector3 targetWorldPos = handCenter;
        
        if (agent.velocity.magnitude > 0.1f) targetWorldPos -= agent.velocity.normalized * bodyLag;
        
        currentSurge = Mathf.Lerp(currentSurge, 0f, Time.deltaTime * 3f);
        targetWorldPos += transform.forward * currentSurge;
        
        float speedFactor = movementController.AnimationSpeedFactor;
        float liftProgress = Mathf.InverseLerp(reachSpeedFactor, pullSpeedFactor, speedFactor);
        float bobY = Mathf.Lerp(-dropAmount, liftAmount, liftProgress);
        
        targetWorldPos.y += bobY;

        Vector3 targetLocalPos = transform.InverseTransformPoint(targetWorldPos);
        targetLocalPos = Vector3.ClampMagnitude(targetLocalPos, 1.0f);
        visualModel.localPosition = Vector3.SmoothDamp(visualModel.localPosition, targetLocalPos, ref bodyVelocity, bodySmoothTime);
        
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float targetPitch = Mathf.Lerp(5f, maxForwardLean, speedRatio);
        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);
        float targetYaw = 0f;
        if (localRight.z < localLeft.z) targetYaw = bodyTwistAmount; else targetYaw = -bodyTwistAmount; 
        Vector3 moveDir = agent.velocity;
        if (moveDir.magnitude < 0.1f) moveDir = transform.forward;
        Quaternion lookRot = Quaternion.LookRotation(moveDir, Vector3.up);
        Quaternion offsetRot = Quaternion.Euler(targetPitch, targetYaw, 0);
        visualModel.rotation = Quaternion.Slerp(visualModel.rotation, lookRot * offsetRot, Time.deltaTime * 6f);
    }

    // Jiggle Fix: Apply positions at the very end
    void UpdateIKTargets()
    {
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
    void OnDrawGizmos()
    {
        // Error Fix: Safety check
        if (this == null || gameObject == null || agent == null) return;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, stableForward * 5f);
    }
}