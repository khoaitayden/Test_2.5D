using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BoolVariableSO isPlayerExposed;
    [SerializeField] private TransformAnchorSO playerAnchor;
    [SerializeField] private IntVariableSO monstersWatchingCount;
    [Header("Settings")]
    [SerializeField] private MonsterConfigBase config;
    [SerializeField] private float detectionFrequency; 
    [SerializeField] private float sightLostDelay; 
    
    [Header("Debug Read-Only")]
    [SerializeField] private bool canSeePlayerNow; 
    [Header("References")]
    [SerializeField] private Transform headBone;
    private MonsterBrain brain;
    private float scanTimer;
    private float timeSinceLastSeen;
    private bool isContributingToCount = false; 
    private Collider[] _overlapBuffer = new Collider[10];

    private void Awake()
    {
        brain = GetComponent<MonsterBrain>();
        if(config == null) config = GetComponent<MonsterConfigBase>();
    }
    private void OnDisable()
    {
        if (isContributingToCount && monstersWatchingCount != null)
        {
            monstersWatchingCount.ApplyChange(-1);
            isContributingToCount = false;
        }
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
        // 1. GLOBAL OVERRIDE CHECK
        if (isPlayerExposed != null && isPlayerExposed.Value)
        {
            
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                brain.OnPlayerSeen(playerAnchor.Value);
                UpdateWatchingStatus(true); // Treat as seen
                return;
            }
        }

        // 2. STANDARD VISION
        Transform seenPlayer = ScanForPlayer();
        bool currentlySeeing = seenPlayer != null;

        if (currentlySeeing)
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

        // 3. UPDATE WISP STATUS
        UpdateWatchingStatus(currentlySeeing);
    }
    private void UpdateWatchingStatus(bool isNowSeeing)
    {
        if (monstersWatchingCount == null) return;

        // State Change Detection
        if (isNowSeeing && !isContributingToCount)
        {
            // Just started seeing
            monstersWatchingCount.ApplyChange(1);
            isContributingToCount = true;
        }
        else if (!isNowSeeing && isContributingToCount)
        {
            // Just stopped seeing
            monstersWatchingCount.ApplyChange(-1);
            isContributingToCount = false;
        }
    }
     private Transform ScanForPlayer()
    {
        // 1. USE HEAD POSITION INSTEAD OF TRANSFORM.POSITION
        Vector3 eyesPosition = headBone != null ? headBone.position : transform.position + Vector3.up * 1.5f;
        Vector3 eyesForward = headBone != null ? headBone.forward : transform.forward;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            config.viewRadius,
            _overlapBuffer,
            config.playerLayerMask
        );

        for (int i = 0; i < count; i++)
        {
            Transform target = _overlapBuffer[i].transform;
            Vector3 targetPosition = target.position + Vector3.up * 1.0f;
            
            Vector3 toTarget = targetPosition - eyesPosition;

            if (Vector3.Angle(eyesForward, toTarget) > config.ViewAngle / 2f)
            {
                continue; 
            }

            float dist = toTarget.magnitude;

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

        Vector3 directLineToPlayer = (end - start).normalized;

        float totalSpreadAngle = 5.0f; 

        int combinedMask = config.obstacleLayerMask | config.playerLayerMask;

        for (int i = 0; i < rays; i++)
        {
            Vector3 finalDir = directLineToPlayer;

            if (rays > 1)
            {

                float t = (i / (float)(rays - 1)) - 0.5f; 
                float angleOffset = t * totalSpreadAngle;

                finalDir = Quaternion.Euler(0, angleOffset, 0) * directLineToPlayer;
            }

            if (Physics.Raycast(start, finalDir, out RaycastHit hit, distance + 2.0f, combinedMask))
            {
                if (((1 << hit.collider.gameObject.layer) & config.obstacleLayerMask) != 0)
                {
                    Debug.DrawRay(start, finalDir * hit.distance, Color.cyan, 0.1f); 
                    continue; 
                }

                if (((1 << hit.collider.gameObject.layer) & config.playerLayerMask) != 0)
                {
                    Debug.DrawRay(start, finalDir * hit.distance, Color.red, 0.1f);
                    return true; 
                }
            }

            Debug.DrawRay(start, finalDir * distance, Color.gray, 0.1f);
        }

        return false;
    }

    public void ForceRevealPlayer(Transform player)
        {
            if (player == null) return;

            timeSinceLastSeen = 0f;
            canSeePlayerNow = true;

            if (brain != null)
            {
                brain.OnPlayerSeen(player);
            }
        }
    private void OnDrawGizmosSelected()
    {
    #if UNITY_EDITOR

        Transform viewSource = headBone != null ? headBone : transform;
        Vector3 origin = viewSource.position;
        Vector3 forward = viewSource.forward;

        UnityEditor.Handles.color = new Color(1, 1, 0, 0.3f); 

        Vector3 leftEdgeDirection = Quaternion.Euler(0, -config.ViewAngle / 2, 0) * forward;
        Vector3 rightEdgeDirection = Quaternion.Euler(0, config.ViewAngle / 2, 0) * forward;

        Vector3 leftPoint = origin + leftEdgeDirection * config.viewRadius;
        Vector3 rightPoint = origin + rightEdgeDirection * config.viewRadius;

        UnityEditor.Handles.DrawLine(origin, leftPoint);
        UnityEditor.Handles.DrawLine(origin, rightPoint);


        UnityEditor.Handles.DrawWireArc(origin, Vector3.up, leftEdgeDirection, config.ViewAngle, config.viewRadius);
    #endif
    }
}