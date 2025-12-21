using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using CrashKonijn.Goap.MonsterGen.Capabilities;

public class ProceduralSmoothClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MonsterMovement movementController;
    [SerializeField] private Transform visualModel; 

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftElbowHint;
    public Transform rightElbowHint;

    [Header("Gait Settings")]
    public float stepStride = 2.0f; // Long, loping strides
    public float handMoveDuration = 0.35f; 
    public float maxArmLength = 3.2f;

    [Header("Physics Feel")]
    [Tooltip("How much the body leans forward when pulling.")]
    public float forwardLeanAmount = 35f; 
    
    [Tooltip("Smoothing for body rotation. Higher = Lazier/Heavier.")]
    public float bodyRotSmoothTime = 8f;

    [Header("Vision")]
    public LayerMask treeLayer;
    public float searchRadius = 12.0f;
    [Range(0, 90)] public float searchAngleWidth = 85f;

    // --- State ---
    private Vector3 leftHandPos;
    private Quaternion leftHandRot;
    private Collider leftTreeCollider; 
    private Vector3 rightHandPos;
    private Quaternion rightHandRot;
    private Collider rightTreeCollider; 

    private bool isHandMoving = false;
    private bool nextIsRight = true;
    private Vector3 lastStepRootPosition;

    // Smoothing Variables
    private Quaternion currentBodyRot;
    private float currentSpeedFactor = 1.0f;

    void Start()
    {
        if (movementController == null) movementController = GetComponent<MonsterMovement>();

        leftHandPos = leftHandTarget.position;
        leftHandRot = leftHandTarget.rotation;
        rightHandPos = rightHandTarget.position;
        rightHandRot = rightHandTarget.rotation;

        DetectInitialTree(leftHandTarget.position, ref leftTreeCollider);
        DetectInitialTree(rightHandTarget.position, ref rightTreeCollider);
        lastStepRootPosition = transform.position;

        currentBodyRot = visualModel.localRotation;
    }

    void DetectInitialTree(Vector3 pos, ref Collider treeRef)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 1.0f, treeLayer);
        if (hits.Length > 0) treeRef = hits[0];
    }

    void LateUpdate()
    {
        // 1. Logic Check
        // Ensure we don't trigger logic if we are barely moving (idle)
        if (!isHandMoving && movementController.AnimationSpeedFactor > 0.1f)
        {
            CheckGaitLogic();
            CheckEmergencyBreak();
        }

        // 2. Visuals
        UpdateBodyTiltAndSway();
        UpdateIKPositions();
    }

    void UpdateBodyTiltAndSway()
    {
        if (visualModel == null) return;

        // --- POSITION SWAY ---
        // Gentle sway opposite to the moving hand
        Vector3 targetLocalPos = Vector3.zero;
        if (isHandMoving) targetLocalPos.x = nextIsRight ? -0.3f : 0.3f;
        visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetLocalPos, Time.deltaTime * 3f);

        // --- ROTATION (The Lean Fix) ---
        
        // 1. Forward Lean (Pitch) based on Speed
        // Access the actual factor from the movement controller
        float speedRatio = movementController.AnimationSpeedFactor; // 0.0 to 2.0+
        float targetPitch = speedRatio * forwardLeanAmount; 

        // 2. Twist (Yaw) towards the anchor hand
        float targetYaw = 0f;
        if (isHandMoving) targetYaw = nextIsRight ? 10f : -10f;

        Quaternion targetRot = Quaternion.Euler(targetPitch, targetYaw, 0);

        // 3. Apply Smoothly
        visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRot, Time.deltaTime * bodyRotSmoothTime);
    }

    void CheckGaitLogic()
    {
        float distMoved = Vector3.Distance(transform.position, lastStepRootPosition);
        if (distMoved > stepStride)
        {
            if (AttemptStep(nextIsRight))
            {
                lastStepRootPosition = transform.position;
                nextIsRight = !nextIsRight;
            }
        }
    }

    void CheckEmergencyBreak()
    {
        float distLeft = Vector3.Distance(transform.position, leftHandPos);
        float distRight = Vector3.Distance(transform.position, rightHandPos);
        if (distRight > maxArmLength) AttemptStep(true);
        else if (distLeft > maxArmLength) AttemptStep(false);
    }

    bool AttemptStep(bool isRightHand)
    {
        Collider bestTree = ScanForTreeInLane(isRightHand);
        if (bestTree != null)
        {
            StartCoroutine(MoveHandRoutine(isRightHand, bestTree));
            return true;
        }
        return false;
    }

    Collider ScanForTreeInLane(bool isRightHand)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, treeLayer);
        Collider bestCandidate = null;
        float bestScore = float.MinValue;

        Vector3 bodyForward = transform.forward;
        Vector3 bodyRight = transform.right;

        foreach (var hit in hits)
        {
            if (hit == leftTreeCollider || hit == rightTreeCollider) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;
            float sideDot = Vector3.Dot(dirToTree, bodyRight);

            if (isRightHand && sideDot < -0.1f) continue; 
            else if (!isRightHand && sideDot > 0.1f) continue;

            if (Vector3.Angle(bodyForward, dirToTree) > searchAngleWidth) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            float forwardDot = Vector3.Dot(bodyForward, dirToTree);
            
            float score = (forwardDot * 5.0f) - (dist * 0.2f);
            if (score > bestScore) { bestScore = score; bestCandidate = hit; }
        }
        return bestCandidate;
    }

    IEnumerator MoveHandRoutine(bool isRight, Collider targetTree)
    {
        isHandMoving = true;

        // --- PHASE 1: SLOW DOWN (But don't stop!) ---
        // Instead of hard 0, we go to 0.2. This keeps the monster gliding.
        // We use a Coroutine loop to ramp speed down smoothly.
        float rampTime = 0.15f;
        float rT = 0;
        float startSpeed = movementController.AnimationSpeedFactor;
        
        while(rT < 1f)
        {
            rT += Time.deltaTime / rampTime;
            // SmoothStep makes the transition non-linear (Ease-In/Out)
            float tSmooth = Mathf.SmoothStep(0, 1, rT); 
            movementController.AnimationSpeedFactor = Mathf.Lerp(startSpeed, 0.2f, tSmooth);
            yield return null;
        }

        // --- PHASE 2: MOVE HAND (With Curves) ---
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

        float t = 0;
        bool hasTriggeredSurge = false;

        while (t < 1f)
        {
            t += Time.deltaTime / handMoveDuration;
            
            // KEY FIX: SmoothStep for Hand Position
            // This prevents the robotic linear movement. Hand moves Slow -> Fast -> Slow.
            float tSmooth = t * t * (3f - 2f * t); 

            Vector3 currentPos = Vector3.Lerp(startPos, finalPos, tSmooth);
            
            // Nice Arc
            float arcHeight = Mathf.Sin(tSmooth * Mathf.PI) * 1.2f; 
            currentPos.y += arcHeight; 
            
            Vector3 sideDir = isRight ? transform.right : -transform.right;
            currentPos += sideDir * Mathf.Sin(tSmooth * Mathf.PI) * 0.6f;

            if (isRight) { rightHandPos = currentPos; rightHandRot = Quaternion.Slerp(startRot, finalRot, tSmooth); }
            else         { leftHandPos = currentPos;  leftHandRot = Quaternion.Slerp(startRot, finalRot, tSmooth); }

            // --- PHASE 2.5: EARLY SURGE ---
            // Start pulling the body BEFORE the hand actually arrives.
            // This blends the animation so there is no gap.
            if (t > 0.8f && !hasTriggeredSurge)
            {
                hasTriggeredSurge = true;
                StartCoroutine(SurgeBodyForward());
            }

            yield return null;
        }

        if (isRight) { rightTreeCollider = targetTree; rightHandPos = finalPos; }
        else         { leftTreeCollider = targetTree;  leftHandPos = finalPos; }

        isHandMoving = false;
    }

    IEnumerator SurgeBodyForward()
    {
        // --- PHASE 3: SURGE (Lunge) ---
        // Ramp speed from 0.2 -> 2.0 -> 1.0
        float surgeDuration = 0.4f;
        float sT = 0;
        
        while(sT < 1f)
        {
            sT += Time.deltaTime / surgeDuration;
            
            // Create a "Hump" curve: Starts low, goes high, ends normal
            // 0.0 -> 0.0
            // 0.5 -> 1.0 (Peak)
            // 1.0 -> 0.5 (Settle)
            float curve = Mathf.Sin(sT * Mathf.PI); 
            
            // Base speed 1.0 + Boost
            // At peak, speed is 1.0 + 1.2 = 2.2x normal speed
            float targetSpeed = 1.0f + (curve * 1.2f);
            
            movementController.AnimationSpeedFactor = targetSpeed;
            yield return null;
        }

        movementController.AnimationSpeedFactor = 1.0f; 
    }

    void UpdateIKPositions()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;

        if (leftElbowHint) leftElbowHint.position = transform.position + (-transform.right * 1.5f) - (transform.forward * 0.5f);
        if (rightElbowHint) rightElbowHint.position = transform.position + (transform.right * 1.5f) - (transform.forward * 0.5f);
    }
}