using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();

            // 1. Keep existing target if we haven't reached it yet
            // This prevents switching targets mid-walk
            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) > config.stoppingDistance * 2f)
            {
                return existingTarget;
            }

            // 2. Try to find a valid, REACHABLE random point
            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            return null;
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            NavMeshPath path = new NavMeshPath();

            // Try 30 times to find a valid point
            for (int i = 0; i < 30; i++)
            {
                // A. Random Point in Donut Shape (Min/Max Distance)
                Vector2 rndDir = Random.insideUnitCircle.normalized;
                float rndDist = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(rndDir.x, 0, rndDir.y) * rndDist;

                // B. Snap to NavMesh
                // Use the fallback radius to handle rough terrain better
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, config.traceNavMeshFallbackRadius, NavMesh.AllAreas))
                {
                    // C. Distance Check (Verify we didn't snap back too close to feet)
                    if (Vector3.Distance(origin, hit.position) < config.minPatrolDistance) continue;

                    // D. REACHABILITY CHECK (THIS FIXES THE WALL ISSUE)
                    // We calculate the path. If it's 'PathPartial', it means the point is 
                    // inside a closed building or disconnected area. We skip it.
                    if (NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            return hit.position;
                        }
                    }
                }
            }
            return null;
        }
    }
}