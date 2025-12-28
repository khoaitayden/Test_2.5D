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

            // --- THE FIX IS HERE ---
            // If we have a target, BUT we are already standing on it (within stopping distance),
            // we MUST discard it and find a new one.
            if (existingTarget != null)
            {
                float dist = Vector3.Distance(agent.Transform.position, existingTarget.Position);
                // If we are far enough away, keep walking to it.
                // If we are close (dist < 1.0f), we treat it as "Done" and generate a new one.
                if (dist > 1.0f) 
                {
                    return existingTarget;
                }
            }

            // 2. Find new point
            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            return null;
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            // Try 10 times
            for (int i = 0; i < 10; i++)
            {
                Vector2 rndDir = Random.insideUnitCircle.normalized;
                float rndDist = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(rndDir.x, 0, rndDir.y) * rndDist;

                // Simple NavMesh check with Wide Radius
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, config.traceNavMeshFallbackRadius, NavMesh.AllAreas))
                {
                    // --- SAFETY CHECK ---
                    // Verify the point we found isn't right next to us (e.g. snapped back to our feet)
                    if (Vector3.Distance(origin, hit.position) > 5.0f)
                    {
                        return hit.position;
                    }
                }
            }
            return null;
        }
    }
}