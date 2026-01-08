using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BoolVariableSO isPlayerExposed;
    [SerializeField] private TransformAnchorSO playerAnchor;
    [Header("Settings")]
    [SerializeField] private MonsterConfig config;
    [SerializeField] private float detectionFrequency; 
    [SerializeField] private float sightLostDelay; 
    
    [Header("Debug Read-Only")]
    [SerializeField] private bool canSeePlayerNow; 
    [Header("References")]
    [SerializeField] private Transform headBone;
    private MonsterBrain brain;
    private float scanTimer;
    private float timeSinceLastSeen;

    // Reusable array to save memory (GC Optimization)
    private Collider[] _overlapBuffer = new Collider[10];

    private void Awake()
    {
        brain = GetComponent<MonsterBrain>();
        if(config == null) config = GetComponent<MonsterConfig>();
    }

    private void Update()
    {
        scanTimer += Time.deltaTime;
        
        if (scanTimer >= detectionFrequency)
        {
            scanTimer = 0f;
            PerformVisionCheck();
        }
    }

    private void PerformVisionCheck()
    {
        // --- 1. GLOBAL EXPOSURE OVERRIDE ---
        if (isPlayerExposed != null && isPlayerExposed.Value)
        {
            // Access player securely via Anchor
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                brain.OnPlayerSeen(playerAnchor.Value);
                canSeePlayerNow = true;
                return;
            }
        }
        // --- 2. STANDARD VISION LOGIC ---
        // (This code only runs if the Eye is NOT exposing the player)
        Transform seenPlayer = ScanForPlayer();
        canSeePlayerNow = seenPlayer != null;

        if (canSeePlayerNow)
        {
            timeSinceLastSeen = 0f;
            brain.OnPlayerSeen(seenPlayer);
        }
        else
        {
            timeSinceLastSeen += detectionFrequency;
            if (timeSinceLastSeen > sightLostDelay)
            {
                brain.OnPlayerLost();
            }
        }
    }

     private Transform ScanForPlayer()
    {
        // 1. USE HEAD POSITION INSTEAD OF TRANSFORM.POSITION
        Vector3 eyesPosition = headBone != null ? headBone.position : transform.position + Vector3.up * 1.5f;
        Vector3 eyesForward = headBone != null ? headBone.forward : transform.forward;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, // Keep Sphere origin at root (feet) for general range
            config.viewRadius,
            _overlapBuffer,
            config.playerLayerMask
        );

        for (int i = 0; i < count; i++)
        {
            Transform target = _overlapBuffer[i].transform;
            Vector3 targetPosition = target.position + Vector3.up * 1.0f; // Look at player chest
            
            Vector3 toTarget = targetPosition - eyesPosition;
            
            // 2. CHECK ANGLE RELATIVE TO HEAD DIRECTION
            // This allows the monster to see "Sideways" if the head is turned!
            if (Vector3.Angle(eyesForward, toTarget) > config.ViewAngle / 2f)
            {
                continue; 
            }

            float dist = toTarget.magnitude;

            // 3. RAYCAST FROM HEAD
            if (CanHitTargetWithRays(eyesPosition, targetPosition, dist))
            {
                return target;
            }
        }

        return null;
    }

    private bool CanHitTargetWithRays(Vector3 start, Vector3 end, float distance)
    {
        int rays = Mathf.Max(1, config.numOfRayCast);
        
        // 1. Calculate the Perfect Direct Line
        Vector3 directLineToPlayer = (end - start).normalized;

        // 2. Tighten the spread!
        // A 15-degree spread misses the player at long range.
        // Use 3 degrees. This covers the width of a human body at ~50 meters.
        float totalSpreadAngle = 5.0f; 

        int combinedMask = config.obstacleLayerMask | config.playerLayerMask;

        for (int i = 0; i < rays; i++)
        {
            Vector3 finalDir = directLineToPlayer;

            // Only fan out if we have multiple rays
            if (rays > 1)
            {
                // Calculate offset (-0.5 to 0.5)
                float t = (i / (float)(rays - 1)) - 0.5f; 
                float angleOffset = t * totalSpreadAngle;
                
                // ROTATE the direct line around the UP axis
                finalDir = Quaternion.Euler(0, angleOffset, 0) * directLineToPlayer;
            }

            // Cast Ray
            // We add distance + 2.0f to ensure we punch through the collider
            if (Physics.Raycast(start, finalDir, out RaycastHit hit, distance + 2.0f, combinedMask))
            {
                // Did we hit an Obstacle?
                if (((1 << hit.collider.gameObject.layer) & config.obstacleLayerMask) != 0)
                {
                    Debug.DrawRay(start, finalDir * hit.distance, Color.cyan, 0.1f); // Blocked
                    continue; 
                }
                
                // Did we hit the Player?
                if (((1 << hit.collider.gameObject.layer) & config.playerLayerMask) != 0)
                {
                    Debug.DrawRay(start, finalDir * hit.distance, Color.red, 0.1f); // SEEN
                    return true; 
                }
            }
            
            // Missed everything (Gray)
            Debug.DrawRay(start, finalDir * distance, Color.gray, 0.1f);
        }

        return false;
    }

    public void ForceRevealPlayer(Transform player)
        {
            if (player == null) return;

            // Reset the "Lost Sight" timer so the monster doesn't forget
            timeSinceLastSeen = 0f;
            canSeePlayerNow = true;

            if (brain != null)
            {
                // Continuously update the brain with the LIVE position
                brain.OnPlayerSeen(player);
            }
        }
    private void OnDrawGizmosSelected()
    {
    #if UNITY_EDITOR
        // Use Head Bone if available, otherwise fallback to Transform
        Transform viewSource = headBone != null ? headBone : transform;
        Vector3 origin = viewSource.position;
        Vector3 forward = viewSource.forward;

        UnityEditor.Handles.color = new Color(1, 1, 0, 0.3f); // Yellow transparent

        // Calculate Cone Edges relative to HEAD rotation
        Vector3 leftEdgeDirection = Quaternion.Euler(0, -config.ViewAngle / 2, 0) * forward;
        Vector3 rightEdgeDirection = Quaternion.Euler(0, config.ViewAngle / 2, 0) * forward;

        Vector3 leftPoint = origin + leftEdgeDirection * config.viewRadius;
        Vector3 rightPoint = origin + rightEdgeDirection * config.viewRadius;

        // Draw Lines
        UnityEditor.Handles.DrawLine(origin, leftPoint);
        UnityEditor.Handles.DrawLine(origin, rightPoint);

        // Draw Arc
        // Note: Vector3.up here assumes the head rotates around Y primarily. 
        // If your head tilts (X/Z), you might want to use viewSource.up
        UnityEditor.Handles.DrawWireArc(origin, Vector3.up, leftEdgeDirection, config.ViewAngle, config.viewRadius);
    #endif
    }
}