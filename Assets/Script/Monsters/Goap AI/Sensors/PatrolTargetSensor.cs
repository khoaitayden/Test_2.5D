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
            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) > config.stoppingDistance * 2f)
            {
                return existingTarget;
            }

            // 2. Try to find a valid, REACHABLE random point using Config settings
            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            // 3. Return null on failure (triggers idle -> retry)
            return null;
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            NavMeshPath path = new NavMeshPath();

            // Try 30 times to find a valid point
            for (int i = 0; i < 10; i++)
            {
                // A. Generate Random Coordinate
                Vector2 rndDir = Random.insideUnitCircle.normalized;
                float rndDist = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(rndDir.x, 0, rndDir.y) * rndDist;

                Vector3 finalHitPos = Vector3.zero;
                bool foundMesh = false;

                // B. PASS 1: Precision Snap (Use Config Variable)
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, config.traceNavMeshSnapRadius, NavMesh.AllAreas))
                {
                    finalHitPos = hit.position;
                    foundMesh = true;
                }
                // C. PASS 2: Fallback Snap (Use Config Variable)
                // If the random point was inside a thick wall/tree, search wider.
                else if (NavMesh.SamplePosition(candidate, out hit, config.traceNavMeshFallbackRadius, NavMesh.AllAreas))
                {
                    finalHitPos = hit.position;
                    foundMesh = true;
                }

                if (foundMesh)
                {
                    // D. Min Distance Check
                    // Ensure the snapped point didn't get pulled back too close to us
                    if (Vector3.Distance(origin, finalHitPos) < config.minPatrolDistance) continue;

                    // E. REACHABILITY CHECK (Crucial Fix)
                    // Ensure we can actually walk there (not inside a locked room)
                    if (NavMesh.CalculatePath(origin, finalHitPos, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            return finalHitPos;
                        }
                    }
                }
            }
            return null;
        }
    }
}