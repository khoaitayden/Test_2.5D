using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ProceduralIndependentClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Shoulder Trigger")]
    [Tooltip("If a hand is behind this Local Z line, it tries to move.")]
    public float releaseThreshold = 0.1f; 
    
    [Header("Physics")]
    public float handSpeed = 9.0f;
    public float swingArcHeight = 1.0f;
    public float swingOutward = 0.6f;

    [Header("Vision (The Fix)")]
    public LayerMask treeLayer;
    public float minReachDistance = 2.0f; 
    public float maxReachDistance = 8.0f;
    public float searchRadius = 15.0f;
    [Range(0, 180)] public float viewAngle = 140f;

    // --- State ---
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider; 
    
    private bool isHandMoving = false;
    private Vector3 stableForward;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        leftHandPos = leftHandTarget.position;
        leftHandRot = leftHandTarget.rotation;
        rightHandPos = rightHandTarget.position;
        rightHandRot = rightHandTarget.rotation;

        stableForward = transform.forward;

        DetectInitialTree(leftHandTarget.position, ref leftTreeCollider);
        DetectInitialTree(rightHandTarget.position, ref rightTreeCollider);
    }

    void DetectInitialTree(Vector3 pos, ref Collider treeRef)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 1.0f, treeLayer);
        if (hits.Length > 0) treeRef = hits[0];
    }

    void LateUpdate()
    {
        // 1. Look Ahead Smoothing
        Vector3 targetDir = GetPathLookAhead();
        stableForward = Vector3.Slerp(stableForward, targetDir, Time.deltaTime * 6f).normalized;

        // 2. Check Logic (Only if not already animating)
        if (!isHandMoving && agent.velocity.magnitude > 0.1f)
        {
            CheckIndependentLogic();
        }

        UpdateIKPositions();
    }

    Vector3 GetPathLookAhead()
    {
        if (!agent.hasPath || agent.path.corners.Length < 2) return transform.forward;
        Vector3 toCorner = (agent.steeringTarget - transform.position).normalized;
        return Vector3.Slerp(transform.forward, toCorner, 0.6f).normalized;
    }

    void CheckIndependentLogic()
    {
        // 1. Calculate "Depth" for BOTH hands relative to the Forward direction
        // Positive = In Front, Negative = Behind
        Vector3 toRight = rightHandPos - transform.position;
        Vector3 toLeft = leftHandPos - transform.position;

        float rightDepth = Vector3.Dot(toRight, stableForward);
        float leftDepth = Vector3.Dot(toLeft, stableForward);

        // 2. Check who needs to move
        bool rightNeedsMove = rightDepth < releaseThreshold;
        bool leftNeedsMove = leftDepth < releaseThreshold;

        // 3. Prioritize
        if (rightNeedsMove && leftNeedsMove)
        {
            // Both dragging? Move the one FURTHEST back (Lowest depth)
            if (rightDepth < leftDepth) AttemptStep(true);
            else AttemptStep(false);
        }
        else if (rightNeedsMove)
        {
            AttemptStep(true);
        }
        else if (leftNeedsMove)
        {
            AttemptStep(false);
        }
    }

    bool AttemptStep(bool isRightHand)
    {
        Collider bestTree = ScanForTree(isRightHand);
        
        if (bestTree != null)
        {
            StartCoroutine(SwingHandRoutine(isRightHand, bestTree));
            return true;
        }
        return false;
    }

    Collider ScanForTree(bool isRightHand)
    {
        Vector3 searchCenter = transform.position + (stableForward * (maxReachDistance * 0.6f));
        Collider[] hits = Physics.OverlapSphere(searchCenter, maxReachDistance, treeLayer);
        
        Collider bestCandidate = null;
        float bestScore = float.MinValue;

        Vector3 pathRight = Vector3.Cross(Vector3.up, stableForward);

        foreach (var hit in hits)
        {
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if (dist < minReachDistance) continue;

            // --- RELAXED LANE CHECK ---
            // Previously: 0.2 (Strict). Now: -0.2 (Loose).
            // This allows the Right Hand to grab "Center-Left" trees if needed.
            float sideDot = Vector3.Dot(hit.transform.position - transform.position, pathRight);
            if (isRightHand)
            {
                if (sideDot < -0.2f) continue; // Allow slight overlap to left
            }
            else
            {
                if (sideDot > 0.2f) continue; // Allow slight overlap to right
            }

            if (Vector3.Angle(stableForward, dirToTree) > viewAngle / 2f) continue;

            // Scoring
            float forwardDot = Vector3.Dot(stableForward, dirToTree);
            float score = (forwardDot * 10.0f) + (dist * 2.0f);

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = hit;
            }
        }

        return bestCandidate;
    }

    IEnumerator SwingHandRoutine(bool isRight, Collider targetTree)
    {
        isHandMoving = true;

        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; 
        Vector3 finalPos = targetCenter;
        Vector3 surfaceNormal = -dirToTree;

        if (targetTree.Raycast(new Ray(rayOrigin, dirToTree), out RaycastHit hit, 20f))
        {
            finalPos = hit.point;
            surfaceNormal = hit.normal;
        }

        Quaternion finalRot = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

        // Bezier Arc
        Vector3 midPoint = (startPos + finalPos) / 2f;
        Vector3 sideVec = isRight ? transform.right : -transform.right;
        Vector3 controlPoint = midPoint + (Vector3.up * swingArcHeight) + (sideVec * swingOutward);

        float totalDist = Vector3.Distance(startPos, finalPos);
        float currentDist = 0;
        
        while (currentDist < totalDist)
        {
            // Adaptive speed: Faster body = Faster hands
            float dynamicSpeed = Mathf.Max(handSpeed, agent.velocity.magnitude * 2.5f);

            currentDist += Time.deltaTime * dynamicSpeed;
            float t = Mathf.Clamp01(currentDist / totalDist);

            float tCurved = t * t * (3f - 2f * t); 

            Vector3 p0 = startPos;
            Vector3 p1 = controlPoint;
            Vector3 p2 = finalPos;
            
            float u = 1 - tCurved;
            float tt = tCurved * tCurved;
            float uu = u * u;
            
            Vector3 currentPos = (uu * p0) + (2 * u * tCurved * p1) + (tt * p2);

            if (isRight) { rightHandPos = currentPos; rightHandRot = Quaternion.Slerp(startRot, finalRot, tCurved); }
            else         { leftHandPos = currentPos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, tCurved); }

            yield return null;
        }

        if (isRight) { rightTreeCollider = targetTree; rightHandPos = finalPos; }
        else         { leftTreeCollider = targetTree;  leftHandPos = finalPos; }

        isHandMoving = false;
    }

    void UpdateIKPositions()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;
    }
}