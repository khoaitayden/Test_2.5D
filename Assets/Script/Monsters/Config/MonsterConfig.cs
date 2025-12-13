using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{
    [Header("Patrol Behavior")]
    public float patrolDistance = 60f;
    
    [Header("Movement")]
    public float patrolSpeed = 4f;
    public float investigateSpeed = 6f;
    public float chaseSpeed = 8f;
    
    [Tooltip("How close monster needs to be to consider destination 'reached'")]
    public float stoppingDistance;
    
    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 60f;
    [Range(0, 360)] public float ViewAngle = 120f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;


    [Header("Senses - Hearing")]
    [Tooltip("Maximum distance to hear loud noises (Soul Collection, Explosions, etc)")]
    public float hearingRange;
    
    [Header("Investigation")]
    public float investigateRadius = 50f;
    public int investigationPoints = 5;
    public float maxInvestigationTime = 20.0f;
    public float minCoverPointDistance = 20.0f; 
    public int numCoverFinderRayCasts= 16;
    [Header("Chase")]
    public float maxChaseTime = 15.0f;
    private void OnDrawGizmosSelected()
    {
    #if UNITY_EDITOR
            // Vision Cone (Hollow Triangle with Arc)
            Handles.color = new Color(1, 1, 0, 0.5f);
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            
            // Calculate the two edge directions
            Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
            Vector3 rightEdgeDirection = Quaternion.Euler(0, ViewAngle / 2, 0) * forward;
            
            // Calculate end points of the cone edges
            Vector3 leftPoint = origin + leftEdgeDirection * viewRadius;
            Vector3 rightPoint = origin + rightEdgeDirection * viewRadius;
            
            // Draw the two straight edges
            Handles.DrawLine(origin, leftPoint);      // Left edge
            Handles.DrawLine(origin, rightPoint);     // Right edge
            
            // Draw the arc at the end (matches original filled shape)
            Handles.DrawWireArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
            
            // NEW: Hearing Range (Blue Wire Circle)
            Handles.color = new Color(0, 0, 1, 0.3f);
            Handles.DrawWireDisc(origin, Vector3.up, hearingRange);
    #endif
    }
}