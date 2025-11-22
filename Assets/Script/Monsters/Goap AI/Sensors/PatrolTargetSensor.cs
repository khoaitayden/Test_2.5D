using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;
        private PatrolHistory patrolHistory;
        
        // Reduced attempts. If we can't find a spot in 10 tries, the map is probably broken.
        private const int MaxAttempts = 10; 

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            
            // Cache PatrolHistory, add if missing
            if (patrolHistory == null)
            {
                patrolHistory = references.GetCachedComponent<PatrolHistory>();
                if (patrolHistory == null)
                {
                    patrolHistory = agent.Transform.gameObject.AddComponent<PatrolHistory>();
                    patrolHistory.SetMaxHistorySize(config.patrolHistorySize);
                }
            }

            // 1. Fast check: Is the previous target still valid?
            // If we are close to it, we need a new one. If we are far, keep going (Optional optimization)
            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) > 2.0f)
            {
                return existingTarget;
            }

            Vector3? validPosition = GetOptimizedPatrolPosition(agent);

            if (!validPosition.HasValue)
            {
                // If fail, return agent pos to prevent null errors, but log warning
                return new PositionTarget(agent.Transform.position);
            }

            patrolHistory.RecordPatrolPoint(validPosition.Value);
            return new PositionTarget(validPosition.Value);
        }

        private Vector3? GetOptimizedPatrolPosition(IActionReceiver agent)
        {
            Vector3 origin = agent.Transform.position;

            for (int i = 0; i < MaxAttempts; i++)
            {
                // Simple random point in circle
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float randomDistance = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;

                // CHEAP OPERATION: Only check if the point is ON the navmesh. 
                // Do NOT check if a path exists yet. That is too expensive for a Sensor.
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    // Lightweight history check
                    if (patrolHistory.IsTooCloseToRecentPoints(hit.position, config.minDistanceFromRecentPoints))
                    {
                        continue; 
                    }
                    
                    return hit.position;
                }
            }

            return null;
        }
    }
}