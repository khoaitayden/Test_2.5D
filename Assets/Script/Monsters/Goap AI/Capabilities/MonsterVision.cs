using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MonsterConfig config;
    [SerializeField] private float detectionFrequency; 
    [SerializeField] private float sightLostDelay; 
    
    [Header("Debug Read-Only")]
    [SerializeField] private bool canSeePlayerNow; 
    
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
        Transform seenPlayer = ScanForPlayer();
        canSeePlayerNow = (seenPlayer != null);

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
        // 1. Define Eyes Position (Up 1.5f for a better "Head" view, 0.5f might be too low/waist)
        Vector3 eyesPosition = transform.position + Vector3.up * 1.2f;

        // 2. Overlap Check (Broad Phase)
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, // Check from feet is fine for radius
            config.viewRadius,
            _overlapBuffer,
            config.playerLayerMask
        );

        for (int i = 0; i < count; i++)
        {
            Transform target = _overlapBuffer[i].transform;
            
            // Target Center: Aim for the chest/center, not the feet pivot
            Vector3 targetCenter = target.position + Vector3.up * 1.0f;

            Vector3 dirToTarget = (targetCenter - eyesPosition).normalized;
            
            // 3. Angle Check (Is player inside the Vision Cone?)
            if (Vector3.Angle(transform.forward, dirToTarget) < config.ViewAngle / 2f)
            {
                float dist = Vector3.Distance(eyesPosition, targetCenter);

                // 4. Raycast Fan Check (Detail Phase)
                if (CanHitTargetWithRays(eyesPosition, targetCenter, dist))
                {
                    return target;
                }
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
}