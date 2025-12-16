using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{

    [Header("Patrol Behavior")]
    [Tooltip("The MINIMUM distance the monster must travel when patrolling.")]
    public float minPatrolDistance; 
    [Tooltip("The MAXIMUM distance the monster can travel when patrolling.")]
    public float maxPatrolDistance; 
    
    [Header("Movement")]
    public float patrolSpeed;
    public float investigateSpeed;
    public float chaseSpeed;
    
    [Tooltip("How close monster needs to be to consider destination 'reached'")]
    public float stoppingDistance;
    
    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 60f;
    [Range(0, 360)] public float ViewAngle = 120f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    [Header("Senses - Hearing")]
    [Tooltip("Maximum distance to hear loud noises")]
    public float hearingRange;

    [Header("Navigation Safety")]
    [Tooltip("First try: How close to the sound to look for NavMesh (Precision)")]
    public float traceNavMeshSnapRadius = 2.0f; 
    
    [Tooltip("Second try: If sound is invalid, how far to search for ANY valid ground (Fallback)")]
    public float traceNavMeshFallbackRadius = 15.0f; 

    [Header("Investigation")]
    public float investigateRadius = 50f;
    public int investigationPoints = 5;
    public float maxInvestigationTime = 20.0f;
    public float minCoverPointDistance = 20.0f; 
    public int numCoverFinderRayCasts = 16;
    
    [Header("Flee Behavior")]
    public float maxChaseTime = 15.0f;
    
    [Tooltip("How far the monster tries to run away when fleeing")]
    public float fleeRunDistance = 20.0f; 
    private void OnDrawGizmosSelected()
    {
    #if UNITY_EDITOR
            // Vision Cone
            Handles.color = new Color(1, 1, 0, 0.5f);
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
            Vector3 leftPoint = origin + leftEdgeDirection * viewRadius;
            Vector3 rightEdgeDirection = Quaternion.Euler(0, ViewAngle / 2, 0) * forward;
            Vector3 rightPoint = origin + rightEdgeDirection * viewRadius;
            Handles.DrawLine(origin, leftPoint);
            Handles.DrawLine(origin, rightPoint);
            Handles.DrawWireArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
            
            // Hearing Range
            Handles.color = new Color(0, 0, 1, 0.3f);
            Handles.DrawWireDisc(origin, Vector3.up, hearingRange);
    #endif
    }
}