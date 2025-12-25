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

    [Header("Rhythm & Speed (The Pull Feel)")]
    [Tooltip("Speed multiplier while reaching (Slow).")]
    [SerializeField] private float reachSpeedFactor = 0.3f; 
    [Tooltip("Speed multiplier immediately after grab (Fast/Surge).")]
    [SerializeField] private float pullSpeedFactor = 2.2f; 
    [Tooltip("How fast speed returns to normal (1.0).")]
    [SerializeField] private float speedDecay = 2.0f;

    [Header("Visual Physics (Bob & Sway)")]
    [Tooltip("How much the body drops down when reaching.")]
    [SerializeField] private float dropAmount = 0.6f;
    [Tooltip("How much the body lifts up when pulling.")]
    [SerializeField] private float liftAmount = 0.2f;
    [SerializeField] private float bodyLag = 0.4f;
    [SerializeField] private float bodySmoothTime = 0.15f;
    [SerializeField] private float maxForwardLean = 45f;
    [SerializeField] private float bodyTwistAmount = 15f;

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
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider;
    private bool isHandMoving;
    private Vector3 stableForward;
    
    // Physics State
    private Vector3 bodyVelocity; 
    private int leftGripHash, rightGripHash;
    private float currentVerticalForce = 0f; // Tracks visual bob

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
        UpdateStableForward();
        
        // 1. Logic Check
        if (!isHandMoving && agent.velocity.sqrMagnitude > MinVelocity * MinVelocity)
        {
            CheckAndMoveHands();
        }

        // 2. Decay Speed (Return to normal after a surge)
        // If speed > 1, decay fast. If speed < 1 (stuck/slow), decay slow.
        float targetSpeed = 1.0f;
        movementController.AnimationSpeedFactor = Mathf.MoveTowards(
            movementController.AnimationSpeedFactor, 
            targetSpeed, 
            Time.deltaTime * speedDecay
        );
        
        // 3. Physics & Visuals
        UpdateBodyPhysics();
        UpdateIKTargets();
    }

    void UpdateStableForward()
    {
        Vector3 targetDir = agent.hasPath ? GetPathDirection() : transform.forward;
        stableForward = Vector3.Slerp(stableForward, targetDir, Time.deltaTime * logicTurnSpeed).normalized;
    }

    void UpdateBodyPhysics()
    {
        if (visualModel == null) return;

        // --- POSITION (Lag + Bob) ---
        Vector3 handCenter = (leftHandPos + rightHandPos) / 2f;
        Vector3 targetWorldPos = handCenter;

        // 1. Drag (Horizontal)
        if (agent.velocity.magnitude > 0.1f)
        {
            targetWorldPos -= agent.velocity.normalized * bodyLag;
        }

        // 2. Bob (Vertical) - Based on Speed Factor
        // If Speed < 1 (Reaching), we sink. 
        // If Speed > 1 (Pulling), we lift.
        float speedFactor = movementController.AnimationSpeedFactor;
        
        // Map speed (0.3 to 2.2) to height (-0.6 to 0.2)
        // Using LerpInverse to find where we are in the surge cycle
        float liftProgress = Mathf.InverseLerp(reachSpeedFactor, pullSpeedFactor, speedFactor);
        
        // Ease the vertical movement
        float targetY = Mathf.Lerp(-dropAmount, liftAmount, liftProgress);
        
        targetWorldPos.y += targetY;

        // 3. Apply
        Vector3 targetLocalPos = transform.InverseTransformPoint(targetWorldPos);
        targetLocalPos = Vector3.ClampMagnitude(targetLocalPos, 1.0f);

        visualModel.localPosition = Vector3.SmoothDamp(
            visualModel.localPosition, 
            targetLocalPos, 
            ref bodyVelocity, 
            bodySmoothTime
        );

        // --- ROTATION ---
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float targetPitch = Mathf.Lerp(5f, maxForwardLean, speedRatio);

        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);
        
        float targetYaw = 0f;
        if (localRight.z < localLeft.z) targetYaw = bodyTwistAmount; 
        else targetYaw = -bodyTwistAmount; 

        Vector3 moveDir = agent.velocity;
        if (moveDir.magnitude < 0.1f) moveDir = transform.forward;
        
        Quaternion lookRot = Quaternion.LookRotation(moveDir, Vector3.up);
        Quaternion offsetRot = Quaternion.Euler(targetPitch, targetYaw, 0);

        visualModel.rotation = Quaternion.Slerp(visualModel.rotation, lookRot * offsetRot, Time.deltaTime * 6f);
    }

    Vector3 GetPathDirection()
    {
        Vector3 rabbitPoint = GetPointOnPath(lookAheadDist);
        return (rabbitPoint - transform.position).normalized;
    }

    Vector3 GetPointOnPath(float distToLook)
    {
        Vector3[] corners = agent.path.corners;
        if (corners.Length < 2) return transform.position + transform.forward * distToLook;

        Vector3 currentPos = transform.position;
        float distCovered = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 segStart = (i == 0) ? currentPos : corners[i];
            Vector3 segEnd = corners[i + 1];
            float segDist = Vector3.Distance(segStart, segEnd);

            if (distCovered + segDist >= distToLook)
            {
                return Vector3.MoveTowards(segStart, segEnd, distToLook - distCovered);
            }
            distCovered += segDist;
        }
        return corners[corners.Length - 1];
    }

    void CheckAndMoveHands()
    {
        bool rightStressed = IsHandStressed(true);
        bool leftStressed = IsHandStressed(false);

        if (rightStressed && leftStressed)
        {
            float rightDepth = Vector3.Dot(rightHandPos - transform.position, stableForward);
            float leftDepth = Vector3.Dot(leftHandPos - transform.position, stableForward);
            AttemptStep(rightDepth < leftDepth);
        }
        else if (rightStressed) AttemptStep(true);
        else if (leftStressed) AttemptStep(false);
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

    bool AttemptStep(bool isRightHand)
    {
        Collider targetTree = FindBestTree(isRightHand);
        if (targetTree != null)
        {
            StartCoroutine(SwingHand(isRightHand, targetTree));
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

    IEnumerator SwingHand(bool isRight, Collider targetTree)
    {
        isHandMoving = true;

        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        Vector3 finalPos = targetCenter;
        Vector3 surfaceNormal = -dirToTree;

        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, dirToTree);
        if (targetTree.Raycast(ray, out RaycastHit hit, 20f))
        {
            finalPos = hit.point;
            surfaceNormal = hit.normal;
        }

        Quaternion finalRot = Quaternion.LookRotation(-surfaceNormal, transform.up);

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

            // --- 1. SLOW DOWN AGENT (Reaching Phase) ---
            // While hand is in air, slow down the body to simulate effort/reach
            // Smoothly lerp towards 'reachSpeedFactor' (e.g., 0.3)
            movementController.AnimationSpeedFactor = Mathf.Lerp(movementController.AnimationSpeedFactor, reachSpeedFactor, Time.deltaTime * 10f);

            // Grip Anim
            float gripVal = 0f;
            if (t < 0.2f) gripVal = 1f - (t / 0.2f);
            else if (t > 0.8f) gripVal = (t - 0.8f) / 0.2f;
            animator.SetFloat(currentGripHash, gripVal);

            // Bezier
            float u = 1f - smoothT;
            Vector3 pos = u * u * startPos + 2f * u * smoothT * controlPoint + smoothT * smoothT * finalPos;

            if (isRight) { rightHandPos = pos; rightHandRot = Quaternion.Slerp(startRot, finalRot, smoothT); }
            else         { leftHandPos = pos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, smoothT); }

            if (t >= 1.0f) break;
            yield return null;
        }

        // Finalize
        animator.SetFloat(currentGripHash, 1f);

        if (isRight) { rightTreeCollider = targetTree; rightHandPos = finalPos; rightHandRot = finalRot; }
        else         { leftTreeCollider = targetTree;  leftHandPos = finalPos; leftHandRot = finalRot; }

        // --- 2. SURGE AGENT (Pull Phase) ---
        // Impact happened. Boost speed instantly to 'pullSpeedFactor' (e.g., 2.2)
        movementController.AnimationSpeedFactor = pullSpeedFactor;

        isHandMoving = false;
    }

    void UpdateIKTargets()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;
    }
}