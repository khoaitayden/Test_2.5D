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
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 7f;
    public float investigateRushSpeed = 6f;
    public float investigateSearchSpeed = 4f;

    [Header("Movement Acceleration")]
    public float patrolAcceleration = 4f;
    public float chaseAcceleration = 12f;
    public float investigateRushAcceleration = 10f;
    public float investigateSearchAcceleration = 6f;

    [Header("Vision")]
    public int numOfRayCast = 5;
    public float viewRadius = 20f;
    [Range(0, 360)]
    public float ViewAngle = 90f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    [Header("Stuck Detection (Global)")]
    public float stuckDistanceThreshold = 1.5f;
    public float maxStuckTime = 3f;

    [Header("Tactical Investigation")]
    public float investigateRadius = 20f;
    public int investigationPoints = 4;
    public float maxInvestigationTime = 100f;

    // --- UPDATED LOGIC ---
    [Header("Arrival & Timeout Logic")]
    
    [Tooltip("If the monster moves less than this distance (meters) in a check interval, it counts as standing still.")]
    public float minEffectiveMovement = 0.2f;

    [Tooltip("How long (seconds) to effectively stand still before forcing 'Arrival' (skipping point).")]
    public float standStillTime = 1.0f; 

    [Tooltip("Generic NavMesh stopping distance.")]
    public float baseStoppingDistance = 2.0f;

    private void OnDrawGizmosSelected()
    {
    #if UNITY_EDITOR
        Handles.color = new Color(1, 1, 0, 0.2f);
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 leftEdgeDirection = Quaternion.Euler(0, -ViewAngle / 2, 0) * forward;
        Handles.DrawSolidArc(origin, Vector3.up, leftEdgeDirection, ViewAngle, viewRadius);
            
        if (monsterBrain == null) monsterBrain = GetComponent<MonsterBrain>();

        if (monsterBrain != null && monsterBrain.LastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(monsterBrain.LastKnownPlayerPosition, investigateRadius);
        }
    #endif
    }
}