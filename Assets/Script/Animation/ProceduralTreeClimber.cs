using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using CrashKonijn.Goap.MonsterGen.Capabilities;

public class ProceduralSpiderClimber : MonoBehaviour
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

    [Header("Body Physics")]
    public float bodyLag = 0.4f;
    public float bodySpring = 6f;
    public float maxForwardLean = 40f;

    [Header("Reaching Settings")]
    public float handSpeedMultiplier = 4.5f; // Very fast hands
    public float minHandSpeed = 8.0f;
    public float stepCooldown = 0.2f; // Short cooldown for responsiveness
    
    [Tooltip("If the angle between Forward and Hand exceeds this, force a step.")]
    public float maxArmAngle = 100f; // Fixes "Hand behind back" during turns
    
    [Tooltip("Ignore trees closer than this.")]
    public float minReachDistance = 2.0f;

    [Header("Vision")]
    public LayerMask treeLayer;
    public float searchRadius = 15.0f;
    [Range(0, 160)] public float searchAngleWidth = 140f; // Wide vision for turns

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

        // Init positions
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
            // Only check logic if not currently moving a hand
            if (!isHandMoving && Time.time > lastStepTime + stepCooldown)
            {
                CheckClimbingLogic();
            }
        }

        UpdateBodyPhysics();
        UpdateDynamicElbows(); // FIX: New Elbow Logic
        UpdateIKPositions();
    }

    void CheckClimbingLogic()
    {
        // 1. Convert Hands to Local Space
        Vector3 localLeft = transform.InverseTransformPoint(leftHandPos);
        Vector3 localRight = transform.InverseTransformPoint(rightHandPos);

        bool triggerStep = false;
        bool forceOverrideTurn = false;
        bool overrideSideIsRight = false;

        // 2. CHECK ROTATION STRESS (The "Turning" Fix)
        // Calculate angle to hands. If > maxArmAngle, the hand is weirdly behind/sideways.
        float angleLeft = Vector3.Angle(transform.forward, leftHandPos - transform.position);
        float angleRight = Vector3.Angle(transform.forward, rightHandPos - transform.position);

        if (angleRight > maxArmAngle)
        {
            triggerStep = true;
            forceOverrideTurn = true;
            overrideSideIsRight = true; // Right hand is stressed, move it!
        }
        else if (angleLeft > maxArmAngle)
        {
            triggerStep = true;
            forceOverrideTurn = true;
            overrideSideIsRight = false; // Left hand is stressed, move it!
        }
        
        // 3. CHECK SHOULDER LINE (Standard Movement)
        if (!triggerStep)
        {
            // Standard alternating gait
            if (nextIsRight && localRight.z < 0.1f) triggerStep = true;
            else if (!nextIsRight && localLeft.z < 0.1f) triggerStep = true;
        }

        // 4. EXECUTE
        if (triggerStep)
        {
            bool sideToMove = forceOverrideTurn ? overrideSideIsRight : nextIsRight;

            if (AttemptStep(sideToMove))
            {
                // If we forced a move, we set the NEXT turn to the opposite
                nextIsRight = !sideToMove;
                lastStepTime = Time.time;
            }
        }
    }

    void UpdateDynamicElbows()
    {
        // FIX: "Weird Bending"
        // Instead of fixed offsets, we calculate where the elbow SHOULD be geometrically.
        
        // 1. Define Shoulder Positions (Approximate relative to root)
        Vector3 leftShoulder = transform.position + (transform.up * 1.5f) - (transform.right * 0.5f);
        Vector3 rightShoulder = transform.position + (transform.up * 1.5f) + (transform.right * 0.5f);

        // 2. Left Elbow Math
        // Find midpoint between shoulder and hand
        Vector3 midLeft = (leftShoulder + leftHandPos) / 2f;
        // Push elbow OUTWARD (Left) and slightly BACK
        Vector3 leftHintDir = (-transform.right + (-transform.forward * 0.5f)).normalized;
        leftElbowHint.position = midLeft + (leftHintDir * 1.5f);

        // 3. Right Elbow Math
        Vector3 midRight = (rightShoulder + rightHandPos) / 2f;
        // Push elbow OUTWARD (Right) and slightly BACK
        Vector3 rightHintDir = (transform.right + (-transform.forward * 0.5f)).normalized;
        rightElbowHint.position = midRight + (rightHintDir * 1.5f);
    }

    void UpdateBodyPhysics()
    {
        if (visualModel == null) return;

        // Position Logic (Center of Mass)
        Vector3 handCenter = (leftHandPos + rightHandPos) / 2f;
        Vector3 targetWorld = handCenter;

        // Drag
        if (agent.velocity.magnitude > 0.1f)
            targetWorld -= agent.velocity.normalized * bodyLag;
        
        // Sag
        targetWorld.y -= 0.5f; 

        // Apply
        Vector3 targetLocal = transform.InverseTransformPoint(targetWorld);
        targetLocal = Vector3.ClampMagnitude(targetLocal, 0.9f);
        visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetLocal, Time.deltaTime * bodySpring);

        // Rotation Logic (Forward Lean)
        float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / 6f);
        float pitch = Mathf.Lerp(10f, maxForwardLean, speedRatio);
        
        // Twist Logic (Look at velocity)
        Vector3 moveDir = agent.velocity.normalized;
        if (moveDir.magnitude < 0.1f) moveDir = transform.forward;
        
        // We blend the look direction: 50% movement, 50% hand center
        Vector3 lookTarget = (moveDir + (handCenter - transform.position).normalized).normalized;
        
        Quaternion targetRot = Quaternion.LookRotation(lookTarget, Vector3.up);
        // Add the forward lean (Pitch) on top
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

        // Use Steering Target to predict turns
        Vector3 moveDir = agent.velocity.magnitude > 0.5f ? agent.velocity.normalized : transform.forward;
        // Also consider where the NavMesh WANTS to go next (Corner anticipation)
        Vector3 steerDir = (agent.steeringTarget - transform.position).normalized;
        Vector3 searchDir = (moveDir + steerDir).normalized;

        Vector3 bodyRight = transform.right;

        foreach (var hit in hits)
        {
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            
            // Lane Check (Strict)
            float sideDot = Vector3.Dot(dirToTree, bodyRight);
            if (isRightHand && sideDot < -0.1f) continue; 
            else if (!isRightHand && sideDot > 0.1f) continue;

            // Vision Cone
            if (Vector3.Angle(searchDir, dirToTree) > searchAngleWidth) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            
            // Min Reach Distance (Prevents cramping)
            if (dist < minReachDistance) continue; 

            float forwardDot = Vector3.Dot(searchDir, dirToTree);
            
            // Score: Forward > Distance
            float score = (forwardDot * 10.0f) + (dist * 2.0f); 

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
        
        // Slight Slowdown to allow arm to catch up
        movementController.AnimationSpeedFactor = 0.85f; 

        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        // Calc Target
        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * 2.0f;
        
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
        Vector3 controlPoint = midPoint + (Vector3.up * 0.5f) + (sideVec * 0.8f); // Wide outward swing

        float totalDist = Vector3.Distance(startPos, finalPos);
        float currentDist = 0;
        
        while (currentDist < totalDist)
        {
            float currentBodySpeed = Mathf.Max(agent.velocity.magnitude, 2.0f);
            float speed = currentBodySpeed * handSpeedMultiplier;
            speed = Mathf.Max(speed, minHandSpeed);

            currentDist += Time.deltaTime * speed;
            float t = currentDist / totalDist;
            if (t > 1f) t = 1f;

            // SmoothStep
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
        // Elbows updated in UpdateDynamicElbows()
    }
}