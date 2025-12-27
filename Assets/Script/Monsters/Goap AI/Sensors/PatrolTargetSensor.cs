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

            // 1. Keep existing target logic
            if (existingTarget != null)
            {
                return existingTarget;
            }

            // 2. Find new point
            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            // DO NOT return null here if we just failed once. 
            // Returning null causes an immediate re-plan which can look glitchy.
            // However, GOAP handles null by idling, which is better than loop-teleporting.
            return null;
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            NavMeshPath path = new NavMeshPath();

            for (int i = 0; i < 30; i++)
            {
                // A. Random Point
                Vector2 rndDir = Random.insideUnitCircle.normalized;
                float rndDist = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(rndDir.x, 0, rndDir.y) * rndDist;

                NavMeshHit hit;
                bool found = false;

                // B. Snapping Strategy
                // Pass 1: Tight Snap (5.0f is better than using the massive fallback)
                if (NavMesh.SamplePosition(candidate, out hit, 5.0f, NavMesh.AllAreas))
                {
                    found = true;
                }
                // Pass 2: Wide Snap (Only if tight failed)
                else if (NavMesh.SamplePosition(candidate, out hit, config.traceNavMeshFallbackRadius, NavMesh.AllAreas))
                {
                    found = true;
                }

                if (found)
                {
                    float finalDistance = Vector3.Distance(origin, hit.position);
                    
                    // We enforce at least 50% of the minimum patrol distance
                    if (finalDistance < config.minPatrolDistance * 0.5f) continue;
                    // --- CRITICAL FIX END ---

                    // D. Path Calculation
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