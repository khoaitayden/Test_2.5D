using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{
    [Header("Patrol Behavior")]
    public float minPatrolDistance = 20f;
    public float maxPatrolDistance = 60f;
    public int patrolHistorySize = 5;
    public float minDistanceFromRecentPoints = 15f;
    
    [Header("Movement")]
    public float patrolSpeed = 4f;
    public float investigateSpeed = 6f;
    public float chaseSpeed = 8f;
    
    [Tooltip("How quickly the monster accelerates/decelerates")]
    public float acceleration = 8f;
    
    [Tooltip("How close monster needs to be to consider destination 'reached'")]
    public float stoppingDistance = 3.5f;
    
    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 60f;
    [Range(0, 360)] public float ViewAngle = 120f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;
    
    [Header("Investigation")]
    public float investigateRadius = 30f;
    public int investigationPoints = 3;
    public float maxInvestigationTime = 20.0f;
    
    [Header("Chase")]
    public float maxChaseTime = 15.0f;
    
    [Header("Stuck Detection")]
    [Tooltip("How long with zero velocity before considering stuck")]
    public float stuckTimeout = 2.5f;
    
    [Tooltip("Additional tolerance multiplier for investigation points (more forgiving)")]
    public float investigationTolerance = 2.0f;

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Handles.color = new Color(1, 1, 0, 0.2f);
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
        Handles.DrawSolidArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
#endif
    }
}