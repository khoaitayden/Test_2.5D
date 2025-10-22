// FILE TO EDIT: MonsterConfig.cs
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Required for the wire arc gizmo
#endif

public class MonsterConfig : MonoBehaviour
{
    [Header("Patrol")]
    public float minPatrolDistance = 15f;
    public float maxPatrolDistance = 50f;

    [Header("Vision")] // Changed from "Attack" for clarity
    public int numOfRayCast = 5;
    public float viewRadius = 20f;
    [Range(0, 360)] // Use a slider for the angle, it's more convenient
    public float ViewAngle = 90f; // <--- NEW PROPERTY
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask; // <--- NEW PROPERTY for line-of-sight

    [Header("Unstuck Logic")]
    public float maxStuckTime = 5f;
    public float stuckVelocityThreshold = 0.1f;

    [Header("Investigate")] // <--- NEW SECTION
    public float investigateRadius = 7f;
    public int minInvestigatePoints = 3; // number of look-around points
    public int maxInvestigatePoints = 6;
    
    // This will draw the view cone gizmo in the editor when the monster is selected
    private void OnDrawGizmosSelected()
    {
        // Set the color for the Gizmo
        Gizmos.color = Color.yellow;
        Handles.color = new Color(1, 1, 0, 0.2f); // Yellow with transparency for the arc fill

        // Get the position and forward direction of the monster
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        // Calculate the direction of the left and right edges of the cone
        Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
        Vector3 rightEdgeDirection = Quaternion.Euler(0, ViewAngle / 2, 0) * forward;
        
        // Draw the two lines that form the edges of the cone
        Gizmos.DrawLine(origin, origin + leftEdgeDirection * viewRadius);
        Gizmos.DrawLine(origin, origin + rightEdgeDirection * viewRadius);
        
#if UNITY_EDITOR
        // Draw a filled arc to make the cone easier to see
        Handles.DrawSolidArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
#endif
    }
}