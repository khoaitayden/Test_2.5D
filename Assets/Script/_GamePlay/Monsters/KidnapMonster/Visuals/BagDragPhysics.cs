using UnityEngine;

public class DraggingBagIK : MonoBehaviour
{
    public Transform monster;
    public float lagDistance = 0.5f; // How far behind it lags
    public float followSpeed = 3f; // Lower = more drag
    public float groundCheckDistance = 5f;
    public LayerMask groundLayer;
    
    private Vector3 currentPos;
    private Vector3 currentVelocity;
    
    void Start()
    {
        currentPos = transform.position;
        
        // Remove rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }
    
    void LateUpdate()
    {
        // Calculate where bag should be (slightly behind monster's movement)
        Vector3 monsterPos = monster.position;
        Vector3 monsterVelocity = (monsterPos - currentPos).normalized;
        
        // Target is slightly behind monster
        Vector3 targetPos = monsterPos - monsterVelocity * lagDistance;
        
        // Smooth follow
        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * followSpeed);
        
        // Raycast down to find ground
        RaycastHit hit;
        Vector3 rayStart = currentPos + Vector3.up * groundCheckDistance;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance * 2f, groundLayer))
        {
            // Place on ground
            SphereCollider col = GetComponent<SphereCollider>();
            float offset = col != null ? col.radius : 0.5f;
            currentPos.y = hit.point.y + offset;
        }
        
        transform.position = currentPos;
    }
}