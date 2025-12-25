using UnityEngine;
using UnityEngine.AI;
using System.Collections;

class ProceduralSmoothPredictor : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;
    [Header("IK Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Path Smoothing")]
    [Tooltip("How far down the path to look for direction (The 'Rabbit').")]
    [SerializeField] private float lookAheadDist = 5f;
    [Tooltip("How fast the logic vector turns (Lower = Smoother).")]
    [SerializeField] private float logicTurnSpeed = 4f;

    [Header("Release Triggers")]
    [Tooltip("Trigger step if hand is behind this line relative to path.")]
    [SerializeField] private float releaseThreshold = 0.1f;
    [Tooltip("Force step if arm angle > this (Fixes T-Pose on turns).")]
    [SerializeField] private float maxArmAngle = 100f;
    [Tooltip("Force step if hand crosses center line (Fixes crossing arms).")]
    [SerializeField] private float crossBodyThreshold = -0.2f;

    [Header("Physics")]
    [SerializeField] private float handSpeed = 10f;
    [SerializeField] private float swingArcHeight = 1.2f;
    [SerializeField] private float swingOutward = 0.7f;

    [Header("Vision")]
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private float minReachDistance = 2.5f;
    [SerializeField] private float maxReachDistance = 8f;
    [Range(0, 180)] [SerializeField] private float viewAngle = 140f;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("How fast the hand opens/closes relative to the swing duration.")]
    [SerializeField] private float gripSpeed = 5.0f; // Higher = Snappier fingers

    // Hashes for performance
    private int leftGripHash;
    private int rightGripHash;

    // State
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider;
    private bool isHandMoving;
    private Vector3 stableForward;

    // Cached values for optimization
    private static readonly float MinVelocity = 0.1f;
    private static readonly float LaneOverlap = 0.3f; // Allow slight lane crossing for better reach
    private static readonly float PathAlignWeight = 8f;
    private static readonly float DistanceWeight = 2f;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        if (animator == null) animator = GetComponentInChildren<Animator>();
        leftGripHash = Animator.StringToHash("LeftGrip");
        rightGripHash = Animator.StringToHash("RightGrip");

        // Force hands closed at start
        animator.SetFloat(leftGripHash, 1f);
        animator.SetFloat(rightGripHash, 1f);
        leftHandPos = leftHandTarget.position;
        leftHandRot = leftHandTarget.rotation;
        rightHandPos = rightHandTarget.position;
        rightHandRot = rightHandTarget.rotation;
        
        stableForward = transform.forward;
        
        DetectInitialTree(leftHandPos, ref leftTreeCollider);
        DetectInitialTree(rightHandPos, ref rightTreeCollider);
    }

    void DetectInitialTree(Vector3 pos, ref Collider treeRef)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 1f, treeLayer);
        if (hits.Length > 0) treeRef = hits[0];
    }

    void LateUpdate()
    {
        // 1. Update the "Stable Forward" vector (The Rabbit)
        UpdateStableForward();
        
        // 2. Check Logic if moving
        if (!isHandMoving && agent.velocity.sqrMagnitude > MinVelocity * MinVelocity)
        {
            CheckAndMoveHands();
        }
        
        // 3. Apply IK
        UpdateIKTargets();
    }

    void UpdateStableForward()
    {
        Vector3 targetDir = transform.forward;

        if (agent.hasPath)
        {
            targetDir = GetPathDirection();
        }
        
        // Smoothly rotate the logic vector to prevent snapping
        stableForward = Vector3.Slerp(stableForward, targetDir, Time.deltaTime * logicTurnSpeed).normalized;
    }

    Vector3 GetPathDirection()
    {
        Vector3 rabbitPoint = GetPointOnPath(lookAheadDist);
        return (rabbitPoint - transform.position).normalized;
    }

    // Finds a point X meters along the path corners
    Vector3 GetPointOnPath(float distToLook)
    {
        Vector3[] corners = agent.path.corners;
        if (corners.Length < 2)
            return transform.position + transform.forward * distToLook;

        Vector3 currentPos = transform.position;
        float distCovered = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 segStart = (i == 0) ? currentPos : corners[i];
            Vector3 segEnd = corners[i + 1];
            float segDist = Vector3.Distance(segStart, segEnd);

            if (distCovered + segDist >= distToLook)
            {
                float remaining = distToLook - distCovered;
                return Vector3.MoveTowards(segStart, segEnd, remaining);
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
            // If both need to move, move the one furthest behind the "Stable Forward" line
            float rightDepth = Vector3.Dot(rightHandPos - transform.position, stableForward);
            float leftDepth = Vector3.Dot(leftHandPos - transform.position, stableForward);
            
            // Lower depth = further back
            AttemptStep(rightDepth < leftDepth);
        }
        else if (rightStressed) AttemptStep(true);
        else if (leftStressed) AttemptStep(false);
    }

    bool IsHandStressed(bool isRight)
    {
        Vector3 handPos = isRight ? rightHandPos : leftHandPos;
        Vector3 toHand = handPos - transform.position;

        // 1. Angle stress (Fixes T-Pose on turns)
        if (Vector3.Angle(stableForward, toHand) > maxArmAngle)
            return true;

        // 2. Cross-body stress (Fixes arms crossing)
        Vector3 stableRight = Vector3.Cross(Vector3.up, stableForward);
        float sideDist = Vector3.Dot(toHand, stableRight);
        
        bool crossingBody = isRight ? 
            sideDist < crossBodyThreshold : 
            sideDist > -crossBodyThreshold;
        
        if (crossingBody) return true;

        // 3. Depth check (Standard walking trigger)
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
        // Search ahead of the monster along the stable path
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
            
            // Min distance check (prevents cramping)
            if (dist < minReachDistance) continue;

            Vector3 dirToTree = toTree / dist; // Normalized

            // Lane filtering (Rotated by Stable Forward)
            float sideDot = Vector3.Dot(toTree, pathRight);
            if ((isRightHand && sideDot < -LaneOverlap) || 
                (!isRightHand && sideDot > LaneOverlap))
                continue;

            // View angle check
            if (Vector3.Angle(stableForward, dirToTree) > halfViewAngle)
                continue;

            // Score calculation: Prioritize alignment with path + distance
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

        // --- CALC TARGET ---
        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        
        // Raycast from a consistent height relative to the monster
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; 
        Vector3 finalPos = targetCenter;
        Vector3 surfaceNormal = -dirToTree;

        if (targetTree.Raycast(new Ray(rayOrigin, dirToTree), out RaycastHit hit, 20f))
        {
            finalPos = hit.point;
            surfaceNormal = hit.normal;
        }

        // --- FIX 1: USE BODY UP VECTOR ---
        // Using Vector3.up causes wrist flips on steep trees. 
        // Using transform.up keeps the hand aligned with the monster's spine.
        Quaternion finalRot = Quaternion.LookRotation(-surfaceNormal, transform.up);

        // --- BEZIER SETUP ---
        Vector3 midPoint = (startPos + finalPos) * 0.5f;
        Vector3 sideOffset = (isRight ? transform.right : -transform.right) * swingOutward;
        Vector3 controlPoint = midPoint + Vector3.up * swingArcHeight + sideOffset;

        float totalDist = Vector3.Distance(startPos, finalPos);
        float elapsed = 0f;
        
        int currentGripHash = isRight ? rightGripHash : leftGripHash;

        // Loop until we physically pass the duration
        while (elapsed <= totalDist)
        {
            float dynamicSpeed = Mathf.Max(handSpeed, agent.velocity.magnitude * 3f);
            
            // Increment
            elapsed += Time.deltaTime * dynamicSpeed;
            
            // Normalized T (0 to 1)
            float t = Mathf.Clamp01(elapsed / totalDist);
            float smoothT = t * t * (3f - 2f * t); 

            // --- ANIMATOR GRIP ---
            float gripValue = 0f; 
            if (t < 0.2f) gripValue = 1f - (t / 0.2f);
            else if (t > 0.8f) gripValue = (t - 0.8f) / 0.2f;
            animator.SetFloat(currentGripHash, gripValue);

            // --- POSITION & ROTATION ---
            float u = 1f - smoothT;
            Vector3 pos = u * u * startPos + 
                         2f * u * smoothT * controlPoint + 
                         smoothT * smoothT * finalPos;

            // Apply immediately
            if (isRight)
            {
                rightHandPos = pos;
                rightHandRot = Quaternion.Slerp(startRot, finalRot, smoothT);
            }
            else
            {
                leftHandPos = pos;
                leftHandRot = Quaternion.Slerp(startRot, finalRot, smoothT);
            }

            // --- FIX 2: FORCE FINISH ---
            // If we hit 1.0, break the loop naturally. 
            // This ensures the last frame rendered IS the final position.
            if (t >= 1.0f) break;

            yield return null;
        }

        // Final Anchor
        animator.SetFloat(currentGripHash, 1f);

        if (isRight)
        {
            rightTreeCollider = targetTree;
            rightHandPos = finalPos;
            rightHandRot = finalRot;
        }
        else
        {
            leftTreeCollider = targetTree;
            leftHandPos = finalPos;
            leftHandRot = finalRot;
        }

        isHandMoving = false;
    }

    void UpdateIKTargets()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;
    }

    void OnDrawGizmos()
    {
        if (agent == null || !Application.isPlaying) return;
        
        // Visualize Stable forward direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, stableForward * 5f);
        
        // Visualize Look-ahead point
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + stableForward * lookAheadDist, 0.5f);
    }
}