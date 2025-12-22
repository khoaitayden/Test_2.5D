using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using CrashKonijn.Goap.MonsterGen.Capabilities;

public class ProceduralSmartClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MonsterMovement movementController;
    [SerializeField] private Transform visualModel;
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets (Chain IK Targets)")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    
    // NOTE: If using ChainIK, assign your "Chain Hint" targets here. 
    // If you don't use hints, leave empty.
    public Transform leftElbowHint; 
    public Transform rightElbowHint;

    [Header("Body Physics")]
    public float bodyLag = 0.4f;
    public float bodySpring = 6f;
    public float maxForwardLean = 40f;

    [Header("Reaching Settings")]
    public float handSpeedMultiplier = 4.5f; 
    public float minHandSpeed = 8.0f;
    public float stepCooldown = 0.2f; // Min time between steps
    
    // FIX: Distance based on speed. 
    // "0.2" means if Speed is 5, we trigger when hand is 1.0m IN FRONT of shoulder.
    public float predictionFactor = 0.2f; 

    [Header("Vision")]
    public LayerMask treeLayer;
    public float searchRadius = 15.0f;
    [Range(0, 160)] public float searchAngleWidth = 140f; 

    // --- State ---
    private Vector3 leftHandPos;
    private Quaternion leftHandRot;
    private Collider leftTreeCollider; 
    
    private Vector3 rightHandPos;
    private Quaternion rightHandRot;
    private Collider rightTreeCollider; 

    private bool isHandMoving = false;
    private bool nextIsRight = true;
    private float lastStepTime = 0f;

    void Start()
    {
        if (movementController == null) movementController = GetComponent<MonsterMovement>();
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
        if (movementController.AnimationSpeedFactor > 0.05f)
        {
            if (!isHandMoving)
            {
                CheckDoubleDrag(); // 1. CRITICAL: Check Emergency First
                CheckStandardWalking(); // 2. Check Standard Rhythm
            }
        }

        UpdateBodyPhysics();
        UpdateChainHints(); 
        UpdateIKPositions();
    }

    // --- 1. THE "HANDCUFFED" FIX ---
    void CheckDoubleDrag()
    {
        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);

        // If BOTH hands are behind the pivot point (Z < 0)
        // We are "Handcuffed". We MUST step immediately.
        if (localLeft.z < -0.1f && localRight.z < -0.1f)
        {
            // Pick the hand furthest back (Lowest Z)
            bool emergencyRight = localRight.z < localLeft.z;
            
            // Bypass Cooldown check and Force Step
            if (AttemptStep(emergencyRight))
            {
                nextIsRight = !emergencyRight;
                lastStepTime = Time.time;
            }
        }
    }

    // --- 2. STANDARD LOGIC ---
    void CheckStandardWalking()
    {
        // Cooldown Check
        if (Time.time < lastStepTime + stepCooldown) return;

        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);

        // Dynamic Threshold based on speed.
        // Higher Speed = Larger Trigger Distance.
        // This ensures we start the step BEFORE the hand passes the body.
        float currentSpeed = agent.velocity.magnitude;
        float activeThreshold = 0.0f + (currentSpeed * predictionFactor); 

        bool triggerStep = false;
        
        // Only check the hand whose turn it is
        if (nextIsRight)
        {
            // If Right Hand falls behind the predictive line
            if (localRight.z < activeThreshold) triggerStep = true;
        }
        else
        {
            if (localLeft.z < activeThreshold) triggerStep = true;
        }

        if (triggerStep)
        {
            if (AttemptStep(nextIsRight))
            {
                nextIsRight = !nextIsRight;
                lastStepTime = Time.time;
            }
        }
    }

    void UpdateChainHints()
    {
        if (leftElbowHint == null || rightElbowHint == null) return;

        // With Chain IK, bending can get weird if hints are too close.
        // We push them WAY OUT to the sides to keep arms "Open".
        
        Vector3 leftShoulder = transform.position + (transform.up * 1.5f) - (transform.right * 0.5f);
        Vector3 rightShoulder = transform.position + (transform.up * 1.5f) + (transform.right * 0.5f);

        // Push Outward heavily
        leftElbowHint.position = leftShoulder - (transform.right * 2.0f) - (transform.forward * 1.0f);
        rightElbowHint.position = rightShoulder + (transform.right * 2.0f) - (transform.forward * 1.0f);
    }

    void UpdateBodyPhysics()
    {
        if (visualModel == null) return;

        Vector3 handCenter = (leftHandPos + rightHandPos) / 2f;
        Vector3 targetWorld = handCenter;

        if (agent.velocity.magnitude > 0.1f)
            targetWorld -= agent.velocity.normalized * bodyLag;
        
        // Reduce sag for Chain IK (looks more aggressive)
        targetWorld.y -= 0.3f; 

        Vector3 targetLocal = transform.InverseTransformPoint(targetWorld);
        targetLocal = Vector3.ClampMagnitude(targetLocal, 0.85f);
        visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetLocal, Time.deltaTime * bodySpring);

        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float pitch = Mathf.Lerp(10f, maxForwardLean, speedRatio);
        
        // Simple Look Rotation without overly complex twist
        Vector3 moveDir = agent.velocity.normalized;
        if (moveDir.magnitude < 0.1f) moveDir = transform.forward;
        Vector3 lookTarget = (moveDir + (handCenter - transform.position).normalized).normalized;
        
        Quaternion targetRot = Quaternion.LookRotation(lookTarget, Vector3.up);
        targetRot *= Quaternion.Euler(pitch, 0, 0);

        visualModel.rotation = Quaternion.Slerp(visualModel.rotation, targetRot, Time.deltaTime * 5f);
    }

    bool AttemptStep(bool isRightHand)
    {
        Collider bestTree = ScanForNextTree(isRightHand);
        if (bestTree != null)
        {
            StartCoroutine(ReachForTree(isRightHand, bestTree));
            return true;
        }
        return false;
    }

    Collider ScanForNextTree(bool isRightHand)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, treeLayer);
        Collider bestCandidate = null;
        float bestScore = float.MinValue;

        Vector3 lookDir = agent.velocity.magnitude > 0.5f ? agent.velocity.normalized : transform.forward;
        Vector3 rightDir = Vector3.Cross(Vector3.up, lookDir);

        foreach (var hit in hits)
        {
            // Important: Chain IK is finicky with snapping. 
            // We ignore currently held trees to force a clean break.
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            
            float sideDot = Vector3.Dot(dirToTree, rightDir);
            if (isRightHand && sideDot < -0.2f) continue; 
            else if (!isRightHand && sideDot > 0.2f) continue;

            if (Vector3.Angle(lookDir, dirToTree) > searchAngleWidth) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            
            // IGNORE close trees to prevent cramping
            if (dist < 2.5f) continue; 

            float forwardDot = Vector3.Dot(lookDir, dirToTree);
            float score = (forwardDot * 12.0f) + (dist * 2.5f); // Heavily favor Distance

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = hit;
            }
        }
        return bestCandidate;
    }

    IEnumerator ReachForTree(bool isRight, Collider targetTree)
    {
        isHandMoving = true;
        movementController.AnimationSpeedFactor = 0.95f; 

        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        
        // Grab High
        Vector3 rayOrigin = transform.position + Vector3.up * 1.8f;
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
        
        // High, Wide control point to prevent Chain IK from flipping
        Vector3 controlPoint = midPoint + (Vector3.up * 0.8f) + (sideVec * 1.0f);

        float totalDist = Vector3.Distance(startPos, finalPos);
        float currentDist = 0;
        
        while (currentDist < totalDist)
        {
            float currentBodySpeed = Mathf.Max(agent.velocity.magnitude, 2.5f);
            float speed = currentBodySpeed * handSpeedMultiplier;
            speed = Mathf.Max(speed, minHandSpeed);

            currentDist += Time.deltaTime * speed;
            float t = currentDist / totalDist;
            if(t > 1f) t = 1f;

            float tSmooth = t * t * (3f - 2f * t);

            Vector3 currentPos = CalculateBezier(tSmooth, startPos, controlPoint, finalPos);
            
            if (isRight) { rightHandPos = currentPos; rightHandRot = Quaternion.Slerp(startRot, finalRot, tSmooth); }
            else         { leftHandPos = currentPos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, tSmooth); }

            yield return null;
        }

        if (isRight) { rightTreeCollider = targetTree; rightHandPos = finalPos; }
        else         { leftTreeCollider = targetTree;  leftHandPos = finalPos; }

        movementController.AnimationSpeedFactor = 1.0f; 
        isHandMoving = false;
    }

    Vector3 CalculateBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }

    void UpdateIKPositions()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;
    }
}