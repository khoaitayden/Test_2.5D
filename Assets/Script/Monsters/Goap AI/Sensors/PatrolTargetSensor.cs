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
        private NavMeshPath pathCache;

        public override void Created() 
        {
            pathCache = new NavMeshPath();
        }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            config = references.GetCachedComponent<MonsterConfig>();
            
            // FIX: Use standard GetComponent to ensure we find the one added in Inspector
            if (patrolHistory == null) 
                patrolHistory = agent.Transform.GetComponent<PatrolHistory>();

            if (patrolHistory == null)
            {
                Debug.LogError("PatrolHistory component missing on Monster! Please add it in Inspector.");
                return new PositionTarget(agent.Transform.position);
            }

            // Optimization: If we have a target and are far away, keep it.
            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) > 5.0f)
            {
                return existingTarget;
            }

            Vector3? p = FindPoint(agent.Transform.position);
            if (p.HasValue)
            {
                patrolHistory.RecordPatrolPoint(p.Value);
                return new PositionTarget(p.Value);
            }

            return new PositionTarget(agent.Transform.position);
        }

        public override void Update()
        {
        }

        private Vector3? FindPoint(Vector3 origin)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(rnd.x, 0, rnd.y);

                // 1. Check NavMesh
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
                {
                    // 2. Check History
                    if (patrolHistory.IsTooCloseToRecentPoints(hit.position, config.minDistanceFromRecentPoints)) continue;

                    // 3. Check Reachability (Crucial for large agents)
                    if (NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, pathCache))
                    {
                        if (pathCache.status == NavMeshPathStatus.PathComplete)
                            return hit.position;
                    }
                }
            }
            return null;
        }
    }
}