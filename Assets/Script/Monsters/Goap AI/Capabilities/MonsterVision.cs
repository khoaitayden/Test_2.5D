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
        
        // Define the direction
        Vector3 centerDir = (end - start).normalized;

        // How much to spread rays (in degrees). 
        // Example: if 5 rays, spread them within 5 degrees to verify 'peeking'
        float spreadAngle = 15f; // Degrees total spread

        for (int i = 0; i < rays; i++)
        {
            Vector3 finalDir = centerDir;

            if (rays > 1)
            {
                // Calculate offset to fan out the rays
                // t goes from -0.5 to +0.5
                float t = (i / (float)(rays - 1)) - 0.5f; 
                float angleOffset = t * spreadAngle;
                
                // Rotate the direction around the UP axis
                finalDir = Quaternion.Euler(0, angleOffset, 0) * centerDir;
            }

            // Cast the ray
            // Returns TRUE if we hit something.
            if (Physics.Raycast(start, finalDir, out RaycastHit hit, distance, config.obstacleLayerMask))
            {
                // Debug: We hit an obstacle
                Debug.DrawRay(start, finalDir * hit.distance, Color.cyan, 0.1f);
            }
            else
            {
                // Returns FALSE means we hit NO OBSTACLES (Open Air)
                // Since we already know the player is at 'distance', if we didn't hit a wall, we see the player.
                
                Debug.DrawRay(start, finalDir * distance, Color.red, 0.1f); // RED = I SEE YOU
                return true;
            }
        }

        return false;
    }
}