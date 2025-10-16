using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

public class PatrolTargetSensor : LocalTargetSensorBase
{
    private const float PatrolRadius = 10f; // How far the monster can wander from its current position
    private const int MaxAttempts = 10;
    private const float NavMeshSampleDistance = 2f; // How far above/below to search for NavMesh
    private const float MaxVerticalOffset = 1f;     // Max allowed height difference from agent

    public override void Created()
    {
        // Nothing needed here unless you have setup logic
    }

    public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
    {
        Vector3? validPosition = GetRandomValidPosition(agent);

        // If no valid position found, stay in place (or you could return null and handle elsewhere)
        Vector3 targetPosition = validPosition ?? agent.Transform.position;

        if (existingTarget is PositionTarget positionTarget)
        {
            return positionTarget.SetPosition(targetPosition);
        }

        return new PositionTarget(targetPosition);
    }

    public override void Update()
    {
        // Not needed — GOAP calls Sense when needed
    }

    private Vector3? GetRandomValidPosition(IActionReceiver agent)
    {
        Vector3 origin = agent.Transform.position;

        for (int i = 0; i < MaxAttempts; i++)
        {
            // Random point in XZ plane within PatrolRadius
            Vector2 randomOffset = Random.insideUnitCircle * PatrolRadius;
            Vector3 randomPoint = origin + new Vector3(randomOffset.x, 0f, randomOffset.y);

            // Sample the NavMesh around that point
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
            {
                // Optional: reject points that are too high/low compared to agent
                if (Mathf.Abs(hit.position.y - origin.y) <= MaxVerticalOffset)
                {
                    return hit.position;
                }
            }
        }

        // Failed to find a valid point after retries
        return null;
    }
}