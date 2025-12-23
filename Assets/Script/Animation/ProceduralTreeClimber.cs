using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ProceduralConeClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Cone Vision Settings")]
    [Range(0, 180)] public float viewAngle = 110f; // Total width of the cone
    public float minReachDistance = 3.0f; // Don't grab anything closer than this
    public float maxReachDistance = 7.0f; // Maximum reach range
    public LayerMask treeLayer;

    [Header("Movement Trigger")]
    [Tooltip("If hand is behind this local Z line, force a move.")]
    public float dragThreshold = 0.5f; 
    public float stepDuration = 0.3f; 

    // --- State ---
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider; 
    private bool isLeftMoving = false;
    private bool isRightMoving = false;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        leftHandPos = leftHandTarget.position;
        leftHandRot = leftHandTarget.rotation;
        rightHandPos = rightHandTarget.position;
        rightHandRot = rightHandTarget.rotation;

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
        // 1. Calculate the "Path Forward" (Where we are going)
        Vector3 pathForward = agent.velocity.magnitude > 0.1f ? agent.velocity.normalized : transform.forward;
        
        // Look into the turn
        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            Vector3 toCorner = (agent.steeringTarget - transform.position).normalized;
            pathForward = Vector3.Slerp(pathForward, toCorner, 0.8f);
        }

        CheckHand(true, pathForward);  // Right
        CheckHand(false, pathForward); // Left

        UpdateIKPositions();
    }

    void CheckHand(bool isRight, Vector3 pathForward)
    {
        if (isRight && isRightMoving) return;
        if (!isRight && isLeftMoving) return;

        Vector3 currentPos = isRight ? rightHandPos : leftHandPos;
        
        // Logic: Is the hand behind the "Drag Threshold" relative to the Path?
        Vector3 toHand = currentPos - transform.position;
        float forwardDot = Vector3.Dot(toHand, pathForward);

        // If hand is behind the threshold OR too close to body (cramped)
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
        // 1. Get all trees in the max range
        Collider[] hits = Physics.OverlapSphere(transform.position, maxReachDistance, treeLayer);
        
        Collider bestCandidate = null;
        float bestScore = float.MinValue;

        Vector3 pathRight = Vector3.Cross(Vector3.up, pathForward);

        foreach (var hit in hits)
        {
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            float distToTree = Vector3.Distance(transform.position, hit.transform.position);

            // --- FILTER 1: DISTANCE BAND ---
            if (distToTree < minReachDistance) continue; // Too close (Tapping fix)

            // --- FILTER 2: LANE CHECK ---
            float sideDot = Vector3.Dot(hit.transform.position - transform.position, pathRight);
            if (isRight)
            {
                if (sideDot < 0.2f) continue; // Must be on Right side
            }
            else
            {
                if (sideDot > -0.2f) continue; // Must be on Left side
            }

            // --- FILTER 3: CONE ANGLE ---
            float angle = Vector3.Angle(pathForward, dirToTree);
            if (angle > viewAngle / 2f) continue; // Outside the cone

            // --- SCORING ---
            // Preference: High Forward Alignment + Far Distance
            float forwardDot = Vector3.Dot(pathForward, dirToTree);
            
            // Formula: Alignment is x10 importance, Distance is x1 importance
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
        
        // Raycast from slightly up/forward to hit the face of the tree
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
            
            // Basic Arc
            Vector3 currentPos = Vector3.Lerp(startPos, finalPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.5f;

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

    // --- VISUALIZE THE CONE ---
    void OnDrawGizmos()
    {
        if (agent == null) return;

        // Calculate Path Forward
        Vector3 pathForward = agent.velocity.magnitude > 0.1f ? agent.velocity.normalized : transform.forward;
        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            Vector3 toCorner = (agent.steeringTarget - transform.position).normalized;
            pathForward = Vector3.Slerp(pathForward, toCorner, 0.8f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, pathForward * maxReachDistance);

        // Draw Cone Boundaries
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * pathForward;
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * pathForward;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, rightBoundary * maxReachDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftBoundary * maxReachDistance);

        // Draw Min Reach
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minReachDistance);
    }
}