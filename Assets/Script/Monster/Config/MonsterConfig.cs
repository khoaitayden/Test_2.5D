using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{
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
    
    [Tooltip("Speed when searching look points (starts here, decreases over time)")]
    public float investigateSearchSpeed = 4f;
    
    [Tooltip("Minimum search speed (won't go below this)")]
    public float investigateMinSpeed = 2.5f;

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

    [Header("Progressive Investigation")]
    [Tooltip("Starting search radius (tight)")]
    public float investigateStartRadius = 5f;
    
    [Tooltip("Maximum search radius (expanded)")]
    public float investigateMaxRadius = 12f;
    
    [Tooltip("How long before starting to expand search (seconds)")]
    public float expandSearchAfter = 6f;
    
    [Tooltip("How long total before giving up (seconds)")]
    public float maxInvestigationTime = 20f;
    
    [Tooltip("Points generated in first phase (tight search)")]
    public int phase1Points = 3;
    
    [Tooltip("Points generated in second phase (expanded search)")]
    public int phase2Points = 4;
    
    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.yellow;
        Handles.color = new Color(1, 1, 0, 0.2f);

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
        Vector3 rightEdgeDirection = Quaternion.Euler(0, ViewAngle / 2, 0) * forward;
        
        Gizmos.DrawLine(origin, origin + leftEdgeDirection * viewRadius);
        Gizmos.DrawLine(origin, origin + rightEdgeDirection * viewRadius);

        Handles.DrawSolidArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
#endif
    }
}