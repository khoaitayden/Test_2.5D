using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class ProceduralTreeClimber : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;

    [Header("IK Targets (Drag from Rig)")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftElbowHint;
    public Transform rightElbowHint;

    [Header("Climbing Settings")]
    [Tooltip("How far the body can move from a hand before the hand must take a step.")]
    public float maxReachDistance = 2.5f;
    [Tooltip("How fast the hand moves to the new target.")]
    public float handMoveSpeed = 10f;
    [Tooltip("Height offset for the grab point (relative to body height).")]
    public float grabHeightOffset = 1.0f;

    [Header("Tree Vision Settings")]
    public LayerMask treeLayer;
    public float searchRadius = 8.0f;
    [Range(0, 180)] public float searchAngle = 160f;

    // --- State ---
    private Vector3 leftHandPos;
    private Quaternion leftHandRot;
    private Collider leftTreeCollider; // The tree currently held by left hand

    private Vector3 rightHandPos;
    private Quaternion rightHandRot;
    private Collider rightTreeCollider; // The tree currently held by right hand

    private bool isLeftHandMoving = false;
    private bool isRightHandMoving = false;

    // Helper to alternate turns if both are valid
    private bool preferRightHand = true; 

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Initialize hands to current rigged positions so they don't snap wildly at start
        leftHandPos = leftHandTarget.position;
        leftHandRot = leftHandTarget.rotation;
        rightHandPos = rightHandTarget.position;
        rightHandRot = rightHandTarget.rotation;
    }

    void LateUpdate()
    {
        // 1. Check if we need to take a step
        CheckClimbingLogic();

        // 2. Pin hands to their world positions (Animation Rigging requirement)
        UpdateIKPositions();
    }

    void CheckClimbingLogic()
    {
        // If the agent isn't moving much, don't try to climb
        if (agent.velocity.magnitude < 0.1f) return;

        float distToLeft = Vector3.Distance(transform.position, leftHandPos);
        float distToRight = Vector3.Distance(transform.position, rightHandPos);

        // Logic: If body moves too far from a hand, that hand needs to release and grab a new tree.
        
        // Priority: Move the hand that is furthest away (stretched out)
        if (!isRightHandMoving && !isLeftHandMoving)
        {
            // If Right is stretched too far
            if (distToRight > maxReachDistance && (distToRight >= distToLeft || isLeftHandMoving))
            {
                FindAndGrabTree(true);
            }
            // If Left is stretched too far
            else if (distToLeft > maxReachDistance)
            {
                FindAndGrabTree(false);
            }
        }
    }

    void FindAndGrabTree(bool isRightHand)
    {
        // 1. Determine which tree to IGNORE (The one the OTHER hand is holding)
        Collider ignoreTree = isRightHand ? leftTreeCollider : rightTreeCollider;

        // 2. Scan for trees (Vision Logic)
        Collider bestTree = ScanForNextTree(ignoreTree);

        if (bestTree != null)
        {
            StartCoroutine(MoveHandRoutine(isRightHand, bestTree));
        }
        else
        {
            // Fallback: If no OTHER tree is found, we might have to grab the same tree 
            // but higher up to keep moving, otherwise the monster gets stuck.
            // Uncomment the line below if you want this fallback behavior.
            
            // if (ignoreTree != null) StartCoroutine(MoveHandRoutine(isRightHand, ignoreTree));
        }
    }

    // --- THE VISION LOGIC ---
    Collider ScanForNextTree(Collider ignoreTree)
    {
        // Overlap Sphere to find candidates
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, treeLayer);
        
        Collider bestCandidate = null;
        float closestDist = float.MaxValue;
        Vector3 forward = transform.forward;

        foreach (var hit in hits)
        {
            // A. Don't grab the tree the other hand is holding
            if (hit == ignoreTree) continue;

            Vector3 dirToTree = (hit.transform.position - transform.position).normalized;

            // B. Angle Check (Vision Cone) - Ensure tree is somewhat in front of us
            if (Vector3.Angle(forward, dirToTree) < searchAngle / 2f)
            {
                // C. Distance Check
                float d = Vector3.Distance(transform.position, hit.transform.position);
                
                // We prefer trees that are reasonably close but not BEHIND us
                if (d < closestDist)
                {
                    closestDist = d;
                    bestCandidate = hit;
                }
            }
        }

        return bestCandidate;
    }

    IEnumerator MoveHandRoutine(bool isRight, Collider targetTree)
    {
        // Lock semaphore
        if (isRight) isRightHandMoving = true;
        else isLeftHandMoving = true;

        Transform handTarget = isRight ? rightHandTarget : leftHandTarget;
        Vector3 startPos = isRight ? rightHandPos : leftHandPos;
        Quaternion startRot = isRight ? rightHandRot : leftHandRot;

        // --- CALCULATE GRAB POINT ---
        // We want to grab the surface of the tree facing the monster.
        // We do a Raycast from the Monster towards the tree center to find the surface point.
        Vector3 targetCenter = targetTree.bounds.center;
        Vector3 dirToTree = (targetCenter - transform.position).normalized;
        
        Vector3 finalPos = targetCenter;
        Vector3 surfaceNormal = -dirToTree;

        // Raycast to find exact surface point
        if (targetTree.Raycast(new Ray(transform.position + Vector3.up * grabHeightOffset, dirToTree), out RaycastHit hit, 20f))
        {
            finalPos = hit.point;
            surfaceNormal = hit.normal;
        }

        // Calculate Rotation (Palm facing tree)
        // Adjust Vector3.up or Vector3.forward depending on your bone orientation
        Quaternion finalRot = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

        // --- ANIMATE ---
        float t = 0;
        // The speed of the hand scales slightly with the agent speed to look natural
        float speed = handMoveSpeed * Mathf.Max(1f, agent.velocity.magnitude / 2f);

        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            
            // Bezier/Arc curve
            Vector3 currentPos = Vector3.Lerp(startPos, finalPos, t);
            // Lift hand higher in the middle of the swing
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.8f; 

            if (isRight)
            {
                rightHandPos = currentPos;
                rightHandRot = Quaternion.Slerp(startRot, finalRot, t);
            }
            else
            {
                leftHandPos = currentPos;
                leftHandRot = Quaternion.Slerp(startRot, finalRot, t);
            }
            
            yield return null;
        }

        // --- FINISH ---
        if (isRight)
        {
            rightTreeCollider = targetTree;
            rightHandPos = finalPos;
            isRightHandMoving = false;
        }
        else
        {
            leftTreeCollider = targetTree;
            leftHandPos = finalPos;
            isLeftHandMoving = false;
        }
    }

    void UpdateIKPositions()
    {
        leftHandTarget.position = leftHandPos;
        leftHandTarget.rotation = leftHandRot;

        rightHandTarget.position = rightHandPos;
        rightHandTarget.rotation = rightHandRot;

        // Hints trail behind slightly
        if (leftElbowHint) 
            leftElbowHint.position = transform.position + (-transform.right * 0.5f) - (transform.forward * 0.5f);
        
        if (rightElbowHint) 
            rightElbowHint.position = transform.position + (transform.right * 0.5f) - (transform.forward * 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
        
        // Visualize Cone
        Vector3 leftRay = Quaternion.Euler(0, -searchAngle / 2, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, searchAngle / 2, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftRay * searchRadius);
        Gizmos.DrawRay(transform.position, rightRay * searchRadius);
    }
}