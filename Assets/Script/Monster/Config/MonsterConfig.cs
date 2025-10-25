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
    [Tooltip("Number of previous patrol points to remember")]
    [Range(3, 10)]
    public int patrolHistorySize = 5;
    
    [Tooltip("Minimum distance from any recent patrol point")]
    public float minDistanceFromRecentPoints = 15f;
    
    [Tooltip("Prefer forward direction? (0 = random, 1 = always forward)")]
    [Range(0f, 1f)]
    public float forwardBias = 0.6f;

    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 20f;
    [Range(0, 360)]
    public float ViewAngle = 90f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    [Header("Stuck Detection")]
    [Tooltip("If monster stays within this radius for maxStuckTime, it's considered stuck")]
    public float stuckDistanceThreshold = 1.5f;
    [Tooltip("How long to wait before declaring the monster stuck (seconds)")]
    public float maxStuckTime = 3f;

    [Header("Investigate")]
    public float investigateRadius = 7f;
    public int minInvestigatePoints = 3;
    public int maxInvestigatePoints = 6;
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Handles.color = new Color(1, 1, 0, 0.2f);

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
        Vector3 rightEdgeDirection = Quaternion.Euler(0, ViewAngle / 2, 0) * forward;
        
        Gizmos.DrawLine(origin, origin + leftEdgeDirection * viewRadius);
        Gizmos.DrawLine(origin, origin + rightEdgeDirection * viewRadius);
        
#if UNITY_EDITOR
        Handles.DrawSolidArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
#endif
    }
}