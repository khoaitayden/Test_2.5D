using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{
    private MonsterBrain monsterBrain; 

    [Header("Patrol")]
    public float minPatrolDistance = 20f;
    public float maxPatrolDistance = 60f;
    
    [Header("Patrol Intelligence")]
    public int patrolHistorySize = 5;
    public float minDistanceFromRecentPoints ;

    [Header("Movement Speeds")]
    public float patrolSpeed = 4f;
    public float chaseSpeed = 8f; 
    public float investigateRushSpeed = 6f;
    
    [Header("Movement Acceleration")]
    public float patrolAcceleration = 5f;
    public float chaseAcceleration = 12f;
    public float investigateRushAcceleration = 8f;
    
    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 60f;
    [Range(0, 360)] public float ViewAngle = 120f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    [Header("Tactical Investigation")]
    public float investigateRadius = 30f;
    public int investigationPoints = 3;
    
    [Header("Timeouts & Limits")]
    [Tooltip("Total time allowed for searching before giving up.")]
    public float maxInvestigationTime = 20.0f; // <-- ADDED BACK

    [Tooltip("If chasing for this long without killing, give up.")]
    public float maxChaseTime = 15.0f; 
    
    [Tooltip("If velocity is near 0 for this long, consider the move 'Complete' (Stuck/Arrived).")]
    public float standStillTimeout = 1.0f;
    
    [Tooltip("Global NavMesh stopping distance.")]
    public float baseStoppingDistance = 2.0f;

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