// FILE TO EDIT: MonsterConfig.cs

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{
    private MonsterBrain monsterBrain; 
    [Header("Patrol")]
    public float minPatrolDistance = 15f;
    public float maxPatrolDistance = 50f;
    
    [Header("Patrol Intelligence")]
    [Range(3, 10)]
    public int patrolHistorySize = 5;
    public float minDistanceFromRecentPoints = 15f;
    [Range(0f, 1f)]
    public float forwardBias = 0.6f;

    [Header("Movement Speeds")]
    [Tooltip("Normal patrol speed")]
    public float patrolSpeed = 3.5f;
    
    [Tooltip("Speed when chasing visible player")]
    public float chaseSpeed = 7f;
    
    [Tooltip("Speed when rushing to last seen position")]
    public float investigateRushSpeed = 6f;
    
    [Tooltip("Speed when searching look points")]
    public float investigateSearchSpeed = 4f;
    
    // REMOVED: investigateMinSpeed is no longer needed as speed doesn't decrease over time.

    [Header("Movement Acceleration")]
    [Tooltip("Acceleration for patrol (smooth, relaxed)")]
    public float patrolAcceleration = 4f;
    
    [Tooltip("Acceleration for chase (fast response, aggressive)")]
    public float chaseAcceleration = 12f;
    
    [Tooltip("Acceleration for investigate rush (quick but controlled)")]
    public float investigateRushAcceleration = 10f;
    
    [Tooltip("Acceleration for search (smooth, cautious)")]
    public float investigateSearchAcceleration = 6f;

    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 20f;
    [Range(0, 360)]
    public float ViewAngle = 90f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    [Header("Stuck Detection")]
    public float stuckDistanceThreshold = 1.5f;
    public float maxStuckTime = 3f;

    // --- REVISED SECTION ---
    [Header("Tactical Investigation")]
    [Tooltip("How far around the player's last position to look for cover points.")]
    public float investigateRadius = 20f;
    
    [Tooltip("The maximum number of cover points the monster will check.")]
    public int investigationPoints = 4;

    [Tooltip("How long total before giving up the search (seconds).")]
    public float maxInvestigationTime = 20f;

    [Header("Investigation Area")]
    [Tooltip("Minimum distance to overshoot past last seen position")]
    public float overshootMinDistance = 3f;

    [Tooltip("Maximum distance to overshoot past last seen position")]
    public float overshootMaxDistance = 8f;

    [Tooltip("How much the direction can vary (degrees) - adds unpredictability")]
    [Range(0f, 45f)]
    public float searchAreaAngleVariance = 20f;
    
    // REMOVED: All phase-related variables (expandSearchAfter, phase1Points, etc.) are gone.
    
    private void OnDrawGizmosSelected()
    {
    #if UNITY_EDITOR
        // --- VISION GIZMO (Unchanged) ---
        Handles.color = new Color(1, 1, 0, 0.2f);
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
        Handles.DrawSolidArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
            
        if (monsterBrain == null)
        {
            monsterBrain = GetComponent<MonsterBrain>();
        }
        // --- NEW INVESTIGATION GIZMO ---
        // Lazily get the reference to the brain component.
        if (monsterBrain == null)
        {
            monsterBrain = GetComponent<MonsterBrain>();
        }

        // Only draw the search radius if the brain has a valid position to investigate.
        if (monsterBrain != null && monsterBrain.LastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            // Draw a wire sphere centered on the last known position.
            Gizmos.DrawWireSphere(monsterBrain.LastKnownPlayerPosition, investigateRadius);
        }
    #endif
    }
}