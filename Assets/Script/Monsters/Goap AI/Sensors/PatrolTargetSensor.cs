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

            // 1. Keep existing target if we are still moving towards it
            // This prevents the monster from jittering/switching targets every frame.
            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) > config.stoppingDistance + 1.0f)
            {
                return existingTarget;
            }

            // 2. Find a new random point
            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            // Fallback: Stay where we are
            return new PositionTarget(agent.Transform.position);
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            // Try 5 times to find a valid spot
            for (int i = 0; i < 5; i++)
            {
                // Random point inside circle
                Vector2 rnd = Random.insideUnitCircle * config.patrolDistance;
                Vector3 candidate = origin + new Vector3(rnd.x, 0, rnd.y);

                // Snap to NavMesh
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
            return null;
        }
    }
}