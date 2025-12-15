using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MonsterConfig config;
    [SerializeField] private float detectionFrequency = 0.1f; 
    [SerializeField] private float sightLostDelay = 1.0f; 
    
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
        Vector3 eyesPosition = transform.position + Vector3.up * 0.5f; // Lift eyes up to chest/head height

        // 1. Physical Overlap (Broad Phase)
        int count = Physics.OverlapSphereNonAlloc(
            eyesPosition,
            config.viewRadius,
            _overlapBuffer,
            config.playerLayerMask
        );

        for (int i = 0; i < count; i++)
        {
            Transform target = _overlapBuffer[i].transform;
            Vector3 targetPosition = target.position + Vector3.up * 0.5f; // Look at player's chest, not feet
            
            Vector3 toTarget = targetPosition - eyesPosition;
            float dist = toTarget.magnitude;

            // 2. Angle Check
            if (Vector3.Angle(transform.forward, toTarget) > config.ViewAngle / 2f)
            {
                continue; // Outside peripheral vision
            }

            // 3. Multi-Raycast Check (Detail Phase)
            if (CanHitTargetWithRays(eyesPosition, targetPosition, dist))
            {
                return target; // Return the first player we see
            }
        }

        return null;
    }

    private bool CanHitTargetWithRays(Vector3 start, Vector3 end, float distance)
    {
        int rays = Mathf.Max(1, config.numOfRayCast);
        Vector3 centerDir = (end - start).normalized;
        
        // Spread angle
        float spreadAngle = 15f; 

        // COMBINE MASKS: We want to hit Walls OR the Player
        // Ensure 'playerLayerMask' is set correctly in Config
        int combinedMask = config.obstacleLayerMask | config.playerLayerMask;

        for (int i = 0; i < rays; i++)
        {
            Vector3 finalDir = centerDir;

            if (rays > 1)
            {
                float t = (i / (float)(rays - 1)) - 0.5f; 
                float angleOffset = t * spreadAngle;
                finalDir = Quaternion.Euler(0, angleOffset, 0) * centerDir;
            }

            // Raycast against EVERYTHING (Walls + Player)
            // We add +1.0f buffer to distance to ensure we pierce the player's collider surface
            if (Physics.Raycast(start, finalDir, out RaycastHit hit, distance + 1.0f, combinedMask))
            {
                // 1. Check if we hit an Obstacle (Wall/Building)
                // (Bitwise check to see if the object's layer is in the obstacle mask)
                if (((1 << hit.collider.gameObject.layer) & config.obstacleLayerMask) != 0)
                {
                    Debug.DrawRay(start, finalDir * hit.distance, Color.cyan, 0.1f);
                    continue; // Blocked by wall, try next ray
                }
                
                // 2. Check if we hit the Player
                if (((1 << hit.collider.gameObject.layer) & config.playerLayerMask) != 0)
                {
                    Debug.DrawRay(start, finalDir * hit.distance, Color.red, 0.1f);
                    return true; // CONFIRMED VISUAL: We actually hit the player!
                }
            }
            
            // If we get here, the ray hit nothing (Empty Air).
            // OLD CODE: This returned true (Bug).
            // NEW CODE: This counts as a miss.
            Debug.DrawRay(start, finalDir * distance, Color.gray, 0.1f);
        }

        return false;
    }
}