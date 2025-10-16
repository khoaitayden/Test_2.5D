using UnityEngine;

public class PatrolConfig : MonoBehaviour
{
    [Header("Wandering Behaviour")]
    [Tooltip("The minimum distance from the agent's current position for a new patrol target.")]
    public float MinPatrolDistance = 15f;
    
    [Tooltip("The maximum distance from the agent's current position for a new patrol target.")]
    public float MaxPatrolDistance = 50f;

    [Header("Unstuck Logic")]
    [Tooltip("How long the agent can be 'stuck' (not moving) before it gives up and finds a new target.")]
    public float MaxStuckTime = 5f;

    [Tooltip("The distance threshold to be considered 'stuck'. If the agent moves less than this distance in a second, it's considered stuck.")]
    public float StuckDistanceThreshold = 0.1f;
}