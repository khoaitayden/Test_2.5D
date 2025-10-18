// ADDED this for the Gizmo
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public class MonsterConfig : MonoBehaviour
{
    [Header("Patrol")]
    public float MinPatrolDistance = 15f;
    public float MaxPatrolDistance = 50f;

    [Header("Attack")]
    public float ViewRadius = 20f;
    // ADDED: The angle for the cone of vision (in degrees).
    [Range(1, 360)]
    public float ViewAngle = 90f; 
    public LayerMask PlayerLayerMask;
    // REMOVED: AttackDistance is no longer needed.
    // public float AttackDistance = 2f; 

    [Header("Unstuck Logic")]
    public float MaxStuckTime = 5f;
    public float StuckVelocityThreshold = 0.1f;
    
    // ADDED: Gizmo to draw the cone of view in the editor.
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw the view cone
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        
        // The color for the cone
        Handles.color = new Color(1f, 1f, 0f, 0.2f); // Yellow, semi-transparent

        // Calculate the start angle for the arc
        Vector3 startLine = Quaternion.AngleAxis(-ViewAngle / 2, Vector3.up) * forward;
        
        // Draw the filled arc
        Handles.DrawSolidArc(position, Vector3.up, startLine, ViewAngle, ViewRadius);

        // Draw the lines for the edges of the cone
        Gizmos.color = new Color(1f, 1f, 0f, 0.9f); // Yellow, opaque
        Gizmos.DrawRay(position, startLine * ViewRadius);
        Gizmos.DrawRay(position, Quaternion.AngleAxis(ViewAngle / 2, Vector3.up) * forward * ViewRadius);
    }
#endif
}