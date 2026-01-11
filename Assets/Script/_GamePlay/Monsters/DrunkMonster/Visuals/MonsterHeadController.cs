using UnityEngine;
using UnityEngine.AI;

public class MonsterHeadController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform headAimTarget; // The IK Target
    [SerializeField] private MonsterBrain brain; // To check if chasing

    [Header("Settings")]
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float lookHeightOffset = 1.5f; // Look at eye level, not floor
    [SerializeField] private float lookAheadDistance = 5.0f;

    // We smooth the actual position to prevent jitter
    private Vector3 currentLookPos;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (brain == null) brain = GetComponent<MonsterBrain>();
        
        // Init
        currentLookPos = transform.position + transform.forward * 5f;
    }

    void LateUpdate()
    {
        Vector3 targetPos = GetDesiredLookTarget();

        // Smoothly move the IK target
        currentLookPos = Vector3.Lerp(currentLookPos, targetPos, Time.deltaTime * turnSpeed);
        
        if (headAimTarget != null)
        {
            headAimTarget.position = currentLookPos;
        }
    }

    Vector3 GetDesiredLookTarget()
    {
        // PRIORITY 1: Look at Player (if we know where they are)
        if (brain.IsPlayerVisible && brain.CurrentPlayerTarget != null)
        {
            // Look slightly below top of player (chest/head area)
            return brain.CurrentPlayerTarget.position + Vector3.up * 1.5f;
        }

        // PRIORITY 2: Look at Navigation Path (The "Look Into Turn" fix)
        if (agent.hasPath && agent.velocity.magnitude > 0.1f)
        {
            // Don't just look at steeringTarget (it snaps). Look further down the path.
            // We reuse the logic from your climbing script roughly here.
            Vector3 steeringDir = (agent.steeringTarget - transform.position).normalized;
            
            // Look far out in that direction at eye level
            Vector3 lookPoint = transform.position + (steeringDir * lookAheadDistance);
            lookPoint.y = transform.position.y + lookHeightOffset; 
            return lookPoint;
        }

        // PRIORITY 3: Default Forward
        return transform.position + (transform.forward * lookAheadDistance) + (Vector3.up * lookHeightOffset);
    }
}