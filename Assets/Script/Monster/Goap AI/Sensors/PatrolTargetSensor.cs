using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
public class PatrolTargetSensor : LocalTargetSensorBase
{
private MonsterConfig config;
// Increased attempts as path validation is stricter and might fail more often.
private const int MaxAttempts = 30;

public override void Created()
{
}

public override void Update()
{
}

public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
{
    // Cache the config on the first run
    if (config == null)
    {
        config = references.GetCachedComponent<MonsterConfig>();
        if (config == null)
        {
            Debug.LogError("PatrolTargetSensor requires a PatrolConfig component on the agent!");
            return null;
        }
    }

    Vector3? validPosition = GetRandomValidReachablePosition(agent);

    // If no valid position found, don't change the target, just wait for the next sense.
    if (!validPosition.HasValue)
        return existingTarget;

    Vector3 targetPosition = validPosition.Value;

    if (existingTarget is PositionTarget positionTarget)
    {
        return positionTarget.SetPosition(targetPosition);
    }

    return new PositionTarget(targetPosition);
}

/// <summary>
/// Finds a random position that is both on the NavMesh AND reachable by the agent.
/// </summary>
private Vector3? GetRandomValidReachablePosition(IActionReceiver agent)
{
    Vector3 origin = agent.Transform.position;

    for (int i = 0; i < MaxAttempts; i++)
    {
        // 1. Get a random point in a wide arc
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(config.MinPatrolDistance, config.MaxPatrolDistance);
        Vector3 randomPoint = origin + new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;

        // 2. Find the nearest point on the NavMesh to our random point
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, config.MaxPatrolDistance, NavMesh.AllAreas))
        {
            // 3. *** CRUCIAL VALIDATION STEP ***
            //    Check if a path can be calculated from the agent to the potential target point.
            var path = new NavMeshPath();
            if (NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path))
            {
                // If the path is complete, it means the destination is reachable.
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    // This is a valid, reachable point!
                    return hit.position;
                }
            }
            // If the path is partial or invalid, we discard this point and try again.
        }
    }
    
    Debug.LogWarning("Could not find a valid AND reachable patrol point after several attempts.");
    return null;
}
}
