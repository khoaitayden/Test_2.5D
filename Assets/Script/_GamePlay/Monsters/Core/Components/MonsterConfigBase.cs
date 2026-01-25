using UnityEngine;

public class MonsterConfigBase : MonoBehaviour
{
    [Header("Core Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 6f;
    public float investigateSpeed = 3.5f;
    [Header("Patrol Behavior")]
    public float minPatrolDistance = 5f; 
    public float maxPatrolDistance = 20f; 
    [Header("Senses")]
    public float viewRadius = 20f;
    [Range(0, 360)] public float ViewAngle = 120f;
    public float hearingRange = 15f;
    
    [Tooltip("How long (seconds) to remember player pos after losing sight")]
    public float memoryDuration = 2.0f;

    [Header("Navigation Safety")]
    public float traceNavMeshSnapRadius = 2.0f; 
    public float traceNavMeshFallbackRadius = 15.0f;

    [Header("Flee Behavior")]
    public float maxChaseTime = 15.0f;
    public float fleeRunDistance = 20.0f; 

    [Header("Dependencies Data")]
    public TraceStorageSO traceStorage;
    public TransformAnchorSO playerAnchor;

    [Header("Layers")]
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;
    
    // Used by MonsterVision
    public int numOfRayCast = 5; 
}