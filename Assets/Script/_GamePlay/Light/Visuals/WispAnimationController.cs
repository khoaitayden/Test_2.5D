using UnityEngine;

public class WispAnimationController : MonoBehaviour
{
    [Header("Architecture")]
    [Tooltip("Reference to the Player's Transform Anchor")]
    [SerializeField] private TransformAnchorSO playerAnchor;

    [Header("References")]
    [Tooltip("Needed to calculate the trail lag effect based on speed")]
    [SerializeField] private PlayerMovement playerMovement; 
    [SerializeField] private Transform mainCameraTransform;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitHeight = 1.5f;
    [SerializeField] private float orbitSpeed = 40f;
    [SerializeField] private float followLag = 0.5f;

    [Header("Lively Motion")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer; 
    [SerializeField] private float collisionRadius = 0.5f;
    [SerializeField] private float avoidanceStrength = 5f;

    [Header("Camera Safety")]
    [SerializeField] private float minDistanceFromCamera = 1.0f;

    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;
    private Collider[] hitColliders = new Collider[5]; 

    void Start()
    {
        if (mainCameraTransform == null && Camera.main != null) 
            mainCameraTransform = Camera.main.transform;
            
        orbitAngle = Random.Range(0f, 360f);
    }

    void LateUpdate()
    {
        // Safety Check: If player is dead/destroyed, stop updating to avoid errors
        if (playerAnchor == null || playerAnchor.Value == null || mainCameraTransform == null) return;

        Transform playerTransform = playerAnchor.Value;

        // 1. Calculate Orbit
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );

        // 2. Calculate Lag (Trail behind player when moving)
        Vector3 lagOffset = Vector3.zero;
        
        // Use PlayerMovement component instead of the God-Class PlayerController
        if (playerMovement != null && playerMovement.IsMoving)
        {
            lagOffset = -playerTransform.forward * followLag;
        }

        Vector3 targetPos = playerTransform.position + orbitOffset + lagOffset + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        // 3. Obstacle Avoidance
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, collisionRadius, hitColliders, obstacleLayer);
        Vector3 avoidance = Vector3.zero;
        if (numHits > 0)
        {
            for (int i = 0; i < numHits; i++)
            {
                if (hitColliders[i] == null) continue;
                Vector3 pushDir = transform.position - hitColliders[i].ClosestPoint(transform.position);
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.up;
                avoidance += pushDir.normalized * (1f - Mathf.Clamp01(pushDir.magnitude / collisionRadius)) * avoidanceStrength;
            }
        }

        // 4. Final Position & Camera Clip
        Vector3 finalPos = targetPos + avoidance;
        Vector3 toWisp = finalPos - mainCameraTransform.position;
        if (toWisp.magnitude < minDistanceFromCamera)
        {
            finalPos = mainCameraTransform.position + toWisp.normalized * minDistanceFromCamera;
        }

        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, 0.2f);

        // 5. Billboarding
        Vector3 flatForward = mainCameraTransform.forward;
        flatForward.y = 0;
        if (flatForward.magnitude > 0.1f) transform.rotation = Quaternion.LookRotation(flatForward);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}