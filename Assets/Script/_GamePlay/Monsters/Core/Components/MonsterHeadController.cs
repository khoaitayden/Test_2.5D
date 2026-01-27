using UnityEngine;
using UnityEngine.AI;

public class MonsterHeadController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform headAimTarget;
    [SerializeField] private MonsterBrain brain;

    [Header("Settings")]
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float lookHeightOffset = 1.5f;
    [SerializeField] private float lookAheadDistance = 5.0f;

    // We smooth the actual position to prevent jitter
    private Vector3 currentLookPos;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (brain == null) brain = GetComponent<MonsterBrain>();

        currentLookPos = transform.position + transform.forward * 5f;
    }

    void LateUpdate()
    {
        Vector3 targetPos = GetDesiredLookTarget();

        currentLookPos = Vector3.Lerp(currentLookPos, targetPos, Time.deltaTime * turnSpeed);
        
        if (headAimTarget != null)
        {
            headAimTarget.position = currentLookPos;
        }
    }

    Vector3 GetDesiredLookTarget()
    {
        if (brain.IsPlayerVisible && brain.CurrentPlayerTarget != null)
        {
            return brain.CurrentPlayerTarget.position + Vector3.up * 1.5f;
        }

        if (agent.hasPath && agent.velocity.magnitude > 0.1f)
        {

            Vector3 steeringDir = (agent.steeringTarget - transform.position).normalized;
            
            Vector3 lookPoint = transform.position + (steeringDir * lookAheadDistance);
            lookPoint.y = transform.position.y + lookHeightOffset; 
            return lookPoint;
        }

        return transform.position + (transform.forward * lookAheadDistance) + (Vector3.up * lookHeightOffset);
    }
}