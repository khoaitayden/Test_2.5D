using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterConfig : MonoBehaviour
{
    [Header("Dependencies")]
    public TraceStorageSO traceStorage;
    [Header("Patrol Behavior")]
    [Tooltip("The MINIMUM distance the monster must travel when patrolling.")]
    public float minPatrolDistance; 
    [Tooltip("The MAXIMUM distance the monster can travel when patrolling.")]
    public float maxPatrolDistance; 
    
    [Header("Movement")]
    public float patrolSpeed;
    public float investigateSpeed;
    public float chaseSpeed;
    
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


    [Header("Stalking Behavior")]
    [Tooltip("Monster will approach this close before starting to orbit.")]
    public float idealStalkingRange = 15f;
    public float minStalkRange = 8f;
    public float maxStalkRange = 25f;
    public float minStalkSpeed = 2f;
    public float maxStalkSpeed = 6f;
}