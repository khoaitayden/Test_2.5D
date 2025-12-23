using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ProceduralSolidConeClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Cone Vision Settings")]
    [Range(0, 180)] public float viewAngle = 100f; 
    public float minReachDistance = 2.5f; 
    public float maxReachDistance = 7.0f; 
    public LayerMask treeLayer;

    [Header("Stability Fix (Look Ahead)")]
    [Tooltip("The cone will look at a point at least this far along the path.")]
    public float minLookAheadDistance = 4.0f; 
    [Tooltip("How fast the cone rotates (lower = smoother, less flicker).")]
    public float coneRotationSpeed = 5.0f;

    [Header("Trigger Logic")]
    public float dragThreshold = 0.5f; 
    public float stepDuration = 0.3f; 

    // --- State ---
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider; 
    private bool isLeftMoving = false;
    private bool isRightMoving = false;

    // The Stabilized Forward Vector
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
        // 1. CALCULATE ROBUST FORWARD
        Vector3 targetDir = GetPathLookAheadDirection();

        // 2. SMOOTH ROTATION (Damping)
        // If the path makes a 90 degree turn, the cone rotates over ~0.5s instead of snapping
        stableForward = Vector3.Slerp(stableForward, targetDir, Time.deltaTime * coneRotationSpeed);
        stableForward.Normalize();

        // 3. LOGIC
        CheckHand(true, stableForward);  // Right
        CheckHand(false, stableForward); // Left

        UpdateIKPositions();
    }

    // --- THE FIX: LOOK AHEAD LOGIC ---
    Vector3 GetPathLookAheadDirection()
    {
        // Fallback: If not moving or no path, use body forward
        if (!agent.hasPath || agent.velocity.magnitude < 0.1f) 
            return transform.forward;

        Vector3 currentPos = transform.position;
        Vector3 targetPoint = transform.position + transform.forward * 5f; // Default far point
        bool foundFarPoint = false;

        // Iterate through the path corners
        Vector3[] corners = agent.path.corners;
        
        // Start from index 1 (Index 0 is current position)
        for (int i = 1; i < corners.Length; i++)
        {
            float dist = Vector3.Distance(currentPos, corners[i]);
            
            // If we found a corner that is far enough away to be stable
            if (dist > minLookAheadDistance)
            {
                targetPoint = corners[i];
                foundFarPoint = true;
                break; // Stop looking, we found our rabbit
            }
        }

        // If all path corners are too close (end of path), just use the last one
        if (!foundFarPoint && corners.Length > 0)
        {
            targetPoint = corners[corners.Length - 1];
        }

        return (targetPoint - currentPos).normalized;
    }

    void CheckHand(bool isRight, Vector3 pathForward)
    {
        if (isRight && isRightMoving) return;
        if (!isRight && isLeftMoving) return;

        Vector3 currentPos = isRight ? rightHandPos : leftHandPos;
        
        // Use the STABLE forward for calculation
        Vector3 toHand = currentPos - transform.position;
        float forwardDot = Vector3.Dot(toHand, pathForward);

        if (forwardDot < dragThreshold || Vector3.Distance(transform.position, currentPos) < 1.5f)
        {
            FindAndGrab(isRight, pathForward);
        }
    }

    void FindAndGrab(bool isRight, Vector3 pathForward)
    {
        Collider bestTree = ScanWithCone(isRight, pathForward);
        
        if (bestTree != null)
        {
            StartCoroutine(MoveHandRoutine(isRight, bestTree));
        }
    }

    Collider ScanWithCone(bool isRight, Vector3 pathForward)
    {
        // Search center projected along the STABLE path
        Vector3 searchCenter = transform.position + (pathForward * (maxReachDistance * 0.6f));
        Collider[] hits = Physics.OverlapSphere(searchCenter, maxReachDistance * 0.8f, treeLayer);
        
        Collider bestCandidate = null;
        float bestScore = float.MinValue;

        Vector3 pathRight = Vector3.Cross(Vector3.up, pathForward);

        foreach (var hit in hits)
        {
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            float distToTree = Vector3.Distance(transform.position, hit.transform.position);

            if (distToTree < minReachDistance) continue; 

            // Lane Check (Using Stable Vector)
            float sideDot = Vector3.Dot(hit.transform.position - transform.position, pathRight);
            if (isRight)
            {
                if (sideDot < 0.2f) continue; 
            }
            else
            {
                if (sideDot > -0.2f) continue; 
            }

            // Cone Angle (Using Stable Vector)
            float angle = Vector3.Angle(pathForward, dirToTree);
            if (angle > viewAngle / 2f) continue;

            // Score
            float forwardDot = Vector3.Dot(pathForward, dirToTree);
            float score = (forwardDot * 10.0f) + distToTree;

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = hit;
            }
        }

        return bestCandidate;
    }

    IEnumerator MoveHandRoutine(bool isRight, Collider targetTree)
    {
        if (isRight) isRightMoving = true; else isLeftMoving = true;

        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f; 
        Vector3 finalPos = targetCenter;
        Vector3 surfaceNormal = -dirToTree;

        if (targetTree.Raycast(new Ray(rayOrigin, dirToTree), out RaycastHit hit, 20f))
        {
            finalPos = hit.point;
            surfaceNormal = hit.normal;
        }

        Quaternion finalRot = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / stepDuration;
            
            Vector3 currentPos = Vector3.Lerp(startPos, finalPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.6f; // Slightly higher arc

            if (isRight) { rightHandPos = currentPos; rightHandRot = Quaternion.Slerp(startRot, finalRot, t); }
            else         { leftHandPos = currentPos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, t); }
            
            yield return null;
        }

        if (isRight) { rightTreeCollider = targetTree; rightHandPos = finalPos; isRightMoving = false; }
        else         { leftTreeCollider = targetTree;  leftHandPos = finalPos; isLeftMoving = false; }
    }

    void UpdateIKPositions()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;
    }

    // --- DEBUG GIZMOS ---
    void OnDrawGizmos()
    {
        if (agent == null) return;

        // Draw the STABLE vector (Cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, stableForward * maxReachDistance);

        // Draw Cone
        Vector3 r = Quaternion.Euler(0, viewAngle/2, 0) * stableForward;
        Vector3 l = Quaternion.Euler(0, -viewAngle/2, 0) * stableForward;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, r * maxReachDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, l * maxReachDistance);
    }
}