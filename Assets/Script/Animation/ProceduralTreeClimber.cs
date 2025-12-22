using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using CrashKonijn.Goap.MonsterGen.Capabilities;

public class ProceduralGaitClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MonsterMovement movementController;
    [SerializeField] private Transform visualModel;
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftElbowHint;
    public Transform rightElbowHint;

    [Header("Gait Settings")]
    [Tooltip("The MINIMUM time between hand movements. This is the main fix for jitter.")]
    public float stepCooldown = 0.4f;
    [Tooltip("Hand moves when it passes this line (Local Z). 0 is the body's center.")]
    public float shoulderLineThreshold = 0.0f;
    [Tooltip("Absolute max distance before an arm is forced to move.")]
    public float maxArmLength = 3.5f;

    [Header("Reach Physics")]
    public float handSpeedMultiplier = 4.0f;
    public float minHandSpeed = 7.0f;

    [Header("Visuals")]
    public float bodyLag = 0.3f;
    public float bodySpring = 7f;
    public float maxForwardLean = 40f;

    [Header("Vision")]
    public LayerMask treeLayer;
    public float searchRadius = 15.0f;
    [Range(0, 160)] public float searchAngleWidth = 120f;

    // --- State ---
    private Vector3 leftHandPos, rightHandPos;
    private Quaternion leftHandRot, rightHandRot;
    private Collider leftTreeCollider, rightTreeCollider;
    private bool isHandMoving = false;
    private bool nextIsRight = true;
    private float lastStepTime = -1f; // Start at -1 to allow immediate first step

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
        if (!isHandMoving && agent.velocity.magnitude > 0.1f)
        {
            // The single, unified logic check.
            CheckAndTriggerStep();
        }

        UpdateBodyPhysics();
        UpdateDynamicElbows();
        UpdateIKPositions();
    }

    // --- UNIFIED LOGIC GATE (THE FIX) ---
    void CheckAndTriggerStep()
    {
        // 1. COOLDOWN GATEKEEPER (HIGHEST PRIORITY)
        // If we are on cooldown, nothing below this line can run.
        if (Time.time < lastStepTime + stepCooldown) return;

        // 2. CALCULATE STATE
        float distRight = Vector3.Distance(transform.position, rightHandPos);
        float distLeft = Vector3.Distance(transform.position, leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);
        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        
        // 3. DETERMINE WHICH HAND TO MOVE (IF ANY)
        bool shouldMoveRight = false;
        bool shouldMoveLeft = false;
        
        // A. Emergency Conditions
        if (distRight > maxArmLength) shouldMoveRight = true;
        if (distLeft > maxArmLength) shouldMoveLeft = true;
        
        // B. Standard Rhythm Conditions (if no emergency)
        if (!shouldMoveRight && !shouldMoveLeft)
        {
            if (nextIsRight && localRight.z < shoulderLineThreshold) shouldMoveRight = true;
            else if (!nextIsRight && localLeft.z < shoulderLineThreshold) shouldMoveLeft = true;
        }

        // 4. EXECUTE STEP
        // If both hands are triggered (emergency), move the one furthest behind.
        if (shouldMoveRight && shouldMoveLeft)
        {
            if (localRight.z < localLeft.z) // Right is further back
            {
                if (AttemptStep(true)) { nextIsRight = false; lastStepTime = Time.time; }
            }
            else // Left is further back
            {
                if (AttemptStep(false)) { nextIsRight = true; lastStepTime = Time.time; }
            }
        }
        else if (shouldMoveRight)
        {
            if (AttemptStep(true)) { nextIsRight = false; lastStepTime = Time.time; }
        }
        else if (shouldMoveLeft)
        {
            if (AttemptStep(false)) { nextIsRight = true; lastStepTime = Time.time; }
        }
    }
    
    // (The rest of the script remains the same)

    void UpdateDynamicElbows()
    {
        if (leftElbowHint == null || rightElbowHint == null) return;
        Vector3 leftShoulder = transform.position + (transform.up * 1.5f) - (transform.right * 0.5f);
        Vector3 rightShoulder = transform.position + (transform.up * 1.5f) + (transform.right * 0.5f);
        Vector3 midLeft = (leftShoulder + leftHandPos) / 2f;
        Vector3 leftHintDir = (-transform.right - transform.forward).normalized;
        leftElbowHint.position = midLeft + (leftHintDir * 1.5f);
        Vector3 midRight = (rightShoulder + rightHandPos) / 2f;
        Vector3 rightHintDir = (transform.right - transform.forward).normalized;
        rightElbowHint.position = midRight + (rightHintDir * 1.5f);
    }
    
    void UpdateBodyPhysics()
    {
        if (visualModel == null) return;
        Vector3 handCenter = (leftHandPos + rightHandPos) / 2f;
        Vector3 targetWorld = handCenter;
        if (agent.velocity.magnitude > 0.1f)
            targetWorld -= agent.velocity.normalized * bodyLag;
        targetWorld.y -= 0.4f;
        Vector3 targetLocal = transform.InverseTransformPoint(targetWorld);
        targetLocal = Vector3.ClampMagnitude(targetLocal, 0.9f);
        visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetLocal, Time.deltaTime * bodySpring);
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float pitch = Mathf.Lerp(10f, maxForwardLean, speedRatio);
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
        Vector3 steerDir = (agent.steeringTarget - transform.position).normalized;
        Vector3 searchDir = (agent.velocity.normalized + steerDir).normalized;
        Vector3 bodyRight = transform.right;

        foreach (var hit in hits)
        {
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;
            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            float sideDot = Vector3.Dot(dirToTree, bodyRight);
            if (isRightHand && sideDot < -0.1f) continue; 
            else if (!isRightHand && sideDot > 0.1f) continue;
            if (Vector3.Angle(searchDir, dirToTree) > searchAngleWidth) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            float forwardDot = Vector3.Dot(searchDir, dirToTree);
            if (forwardDot < 0.5f) continue;
            float idealDist = 4.0f;
            float distScore = 1.0f - Mathf.Abs(dist - idealDist) / idealDist;
            float score = (forwardDot * 3.0f) + distScore; 
            if (score > bestScore) { bestScore = score; bestCandidate = hit; }
        }
        return bestCandidate;
    }

    IEnumerator ReachForTree(bool isRight, Collider targetTree)
    {
        isHandMoving = true;
        movementController.AnimationSpeedFactor = 0.9f; 

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
        Vector3 controlPoint = midPoint + Vector3.up * 1.0f + sideVec * 0.8f;
        float totalDist = Vector3.Distance(startPos, finalPos);
        if (totalDist < 0.1f) totalDist = 0.1f;
        float currentDist = 0;
        
        while (currentDist < totalDist)
        {
            float speed = agent.velocity.magnitude * handSpeedMultiplier;
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