using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ProceduralSmoothPredictor : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Path Smoothing (The Fix)")]
    [Tooltip("How far down the path to look for the direction.")]
    public float lookAheadDist = 5.0f; 
    [Tooltip("How fast the logic vector turns (Lower = Smoother).")]
    public float logicTurnSpeed = 4.0f;

    [Header("Triggers")]
    public float releaseThreshold = 0.1f; 
    public float maxArmAngle = 100f;
    public float crossBodyThreshold = -0.2f;

    [Header("Physics")]
    public float handSpeed = 10.0f;
    public float swingArcHeight = 1.2f;
    public float swingOutward = 0.7f;

    [Header("Vision")]
    public LayerMask treeLayer;
    public float minReachDistance = 2.5f; 
    public float maxReachDistance = 8.0f;
    public float searchRadius = 15.0f;
    [Range(0, 180)] public float viewAngle = 140f;

    // --- State ---
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider; 
    private bool isHandMoving = false;
    
    // The Stabilized Logic Vector
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
        // 1. Update the Smooth Direction
        UpdateStableForward();

        // 2. Logic
        if (!isHandMoving && agent.velocity.magnitude > 0.1f)
        {
            CheckRotationLogic();
        }

        UpdateIKPositions();
    }

    // --- THE SMOOTHING LOGIC ---
    void UpdateStableForward()
    {
        Vector3 targetDir = transform.forward;

        // If we have a path, find the "Rabbit" point
        if (agent.hasPath)
        {
            Vector3 rabbitPoint = GetPointOnPath(lookAheadDist);
            targetDir = (rabbitPoint - transform.position).normalized;
        }

        // Smoothly rotate current logic vector towards target
        // Slerp prevents snapping
        stableForward = Vector3.Slerp(stableForward, targetDir, Time.deltaTime * logicTurnSpeed).normalized;
    }

    // Walks along the path corners to find a point X meters away
    Vector3 GetPointOnPath(float distToLook)
    {
        Vector3[] corners = agent.path.corners;
        if (corners.Length < 2) return transform.position + transform.forward * distToLook;

        Vector3 currentPos = transform.position;
        float distCovered = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 segStart = corners[i];
            Vector3 segEnd = corners[i + 1];
            
            // If checking first segment, start from agent pos, not corner 0
            if (i == 0) segStart = currentPos;

            float segDist = Vector3.Distance(segStart, segEnd);

            if (distCovered + segDist >= distToLook)
            {
                // The target is on this segment
                float remaining = distToLook - distCovered;
                return Vector3.MoveTowards(segStart, segEnd, remaining);
            }

            distCovered += segDist;
        }

        // If path is shorter than look distance, return the very end
        return corners[corners.Length - 1];
    }

    void CheckRotationLogic()
    {
        // Use STABLE forward for all calculations, not body forward
        bool rightNeedsMove = CheckHandStress(true);
        bool leftNeedsMove = CheckHandStress(false);

        if (rightNeedsMove && leftNeedsMove)
        {
            float rightDot = Vector3.Dot(rightHandPos - transform.position, stableForward);
            float leftDot = Vector3.Dot(leftHandPos - transform.position, stableForward);

            // Move the one furthest back relative to the PATH
            if (rightDot < leftDot) AttemptStep(true);
            else AttemptStep(false);
        }
        else if (rightNeedsMove) AttemptStep(true);
        else if (leftNeedsMove) AttemptStep(false);
    }

    bool CheckHandStress(bool isRight)
    {
        Vector3 handPos = isRight ? rightHandPos : leftHandPos;
        Vector3 toHand = handPos - transform.position;

        // 1. Angle Stress (Using Stable Forward)
        if (Vector3.Angle(stableForward, toHand) > maxArmAngle) return true;

        // 2. Cross Body Stress (Using Stable Local Space)
        // We act as if the body is aligned with StableForward to check crossing
        Vector3 stableRight = Vector3.Cross(Vector3.up, stableForward);
        float sideDist = Vector3.Dot(toHand, stableRight);

        if (isRight && sideDist < crossBodyThreshold) return true; 
        if (!isRight && sideDist > -crossBodyThreshold) return true;

        // 3. Depth Check
        float depth = Vector3.Dot(toHand, stableForward);
        if (depth < releaseThreshold) return true;

        return false;
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

            // Lane Check (Rotated by Stable Forward)
            float sideDot = Vector3.Dot(hit.transform.position - transform.position, pathRight);
            
            if (isRightHand)
            {
                if (sideDot < -0.3f) continue; // Allow slight overlap
            }
            else
            {
                if (sideDot > 0.3f) continue; 
            }

            // Angle Check
            if (Vector3.Angle(stableForward, dirToTree) > viewAngle / 2f) continue;

            // Score: Alignment with Path + Turn + Distance
            float pathAlign = Vector3.Dot(stableForward, dirToTree);
            
            float score = (pathAlign * 8.0f) + (dist * 2.0f);

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

        Vector3 midPoint = (startPos + finalPos) / 2f;
        Vector3 sideVec = isRight ? transform.right : -transform.right;
        Vector3 controlPoint = midPoint + (Vector3.up * swingArcHeight) + (sideVec * swingOutward);

        float totalDist = Vector3.Distance(startPos, finalPos);
        float currentDist = 0;
        
        while (currentDist < totalDist)
        {
            float dynamicSpeed = Mathf.Max(handSpeed, agent.velocity.magnitude * 3.0f);

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

    // DEBUG: Visualize the Smooth Logic Vector
    void OnDrawGizmos()
    {
        if (agent == null) return;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, stableForward * 5.0f);
        
        // Visualize the "Rabbit" Point
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + stableForward * lookAheadDist, 0.5f);
    }
}