using UnityEngine;
using UnityEngine.AI;

public class KidnapBagDragAnimationController : MonoBehaviour
{
    [SerializeField] private Transform monster;
    [SerializeField] private float lagDistance = 1f;
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float groundCheckDistance = 5f;
    [SerializeField] private LayerMask groundLayer;
    
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
        
        if (navAgent != null && navAgent.velocity.magnitude > 0.1f)
        {
            movementDirection = navAgent.velocity.normalized;
        }
        else if ((monsterPos - lastMonsterPos).magnitude > 0.01f)
        {
            movementDirection = (monsterPos - lastMonsterPos).normalized;
        }
        else
        {
            movementDirection = monster.forward;
        }

        movementDirection = Vector3.Lerp(movementDirection, movementDirection, Time.deltaTime * 3f);

        Vector3 targetPos = monsterPos - movementDirection * lagDistance;

        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * followSpeed);

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