using UnityEngine;
using UnityEngine.AI;

public class DraggingBagIK : MonoBehaviour
{
    public Transform monster;
    public float lagDistance = 1f;
    public float followSpeed = 3f;
    public float groundCheckDistance = 5f;
    public LayerMask groundLayer;
    
    private Vector3 currentPos;
    private Vector3 lastMonsterPos;
    private Vector3 movementDirection;
    private NavMeshAgent navAgent;
    
    void Start()
    {
        currentPos = transform.position;
        lastMonsterPos = monster.position;
        movementDirection = -monster.forward; 
        
        navAgent = monster.GetComponent<NavMeshAgent>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }
    
    void LateUpdate()
    {
        Vector3 monsterPos = monster.position;
        
        // Determine direction to place bag behind monster
        if (navAgent != null && navAgent.velocity.magnitude > 0.1f)
        {
            // Monster is moving - use velocity direction
            movementDirection = navAgent.velocity.normalized;
        }
        else if ((monsterPos - lastMonsterPos).magnitude > 0.01f)
        {
            // Monster moved but no nav agent - use position change
            movementDirection = (monsterPos - lastMonsterPos).normalized;
        }
        else
        {
            // Monster stopped - use its facing direction
            movementDirection = monster.forward;
        }
        
        // Smooth the direction
        movementDirection = Vector3.Lerp(movementDirection, movementDirection, Time.deltaTime * 3f);
        
        // Target is behind the monster
        Vector3 targetPos = monsterPos - movementDirection * lagDistance;
        
        // Smooth follow
        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * followSpeed);
        
        // Ground raycast
        RaycastHit hit;
        Vector3 rayStart = currentPos + Vector3.up * groundCheckDistance;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance * 2f, groundLayer))
        {
            SphereCollider col = GetComponent<SphereCollider>();
            float offset = col != null ? col.radius : 0.5f;
            currentPos.y = hit.point.y + offset;
        }
        
        transform.position = currentPos;
        lastMonsterPos = monsterPos;
    }
}