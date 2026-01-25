using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        private MonsterConfigBase config;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfigBase>();

            if (existingTarget != null)
            {
                float dist = Vector3.Distance(agent.Transform.position, existingTarget.Position);
                if (dist > 1.0f) 
                {
                    return existingTarget;
                }
            }

            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            return null;
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 rndDir = Random.insideUnitCircle.normalized;
                float rndDist = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                Vector3 candidate = origin + new Vector3(rndDir.x, 0, rndDir.y) * rndDist;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, config.traceNavMeshFallbackRadius, NavMesh.AllAreas))
                {

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