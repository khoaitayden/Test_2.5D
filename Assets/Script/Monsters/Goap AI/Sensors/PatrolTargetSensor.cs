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
        private const int MaxAttempts = 10; 

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            
            if (patrolHistory == null)
            {
                patrolHistory = references.GetCachedComponent<PatrolHistory>();
                if (patrolHistory == null)
                {
                    patrolHistory = agent.Transform.gameObject.AddComponent<PatrolHistory>();
                    patrolHistory.SetMaxHistorySize(config.patrolHistorySize);
                }
            }

            // FIX IS HERE: Used 'baseStoppingDistance' instead of 'patrolStoppingDistance'
            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) > config.baseStoppingDistance + 1f)
            {
                return existingTarget;
            }

            Vector3? validPosition = GetOptimizedPatrolPosition(agent);

            if (!validPosition.HasValue)
            {
                // Warning: Map might be too small for config settings
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
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float randomDistance = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;

                // Increase sample range to 5.0f to find mesh easier for big monsters
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    if (patrolHistory.IsTooCloseToRecentPoints(hit.position, config.minDistanceFromRecentPoints))
                        continue; 
                    
                    if (Vector3.Distance(origin, hit.position) < 5.0f)
                        continue;

                    return hit.position;
                }
            }
            return null;
        }
    }
}