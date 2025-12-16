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

            // 2. Try to find a valid random point
            Vector3? point = GetRandomPoint(agent.Transform.position);
            
            if (point.HasValue)
            {
                return new PositionTarget(point.Value);
            }

            // 3. Return null if failed. 
            // This causes "No Action Found" -> Idle for one frame -> Retry.
            // Much better than returning current position (which causes instant-complete flickering).
            return null;
        }

        private Vector3? GetRandomPoint(Vector3 origin)
        {
            // Try 30 times to find a valid point on the NavMesh
            for (int i = 0; i < 30; i++)
            {
                // A. Get Random Direction
                Vector2 rndDir = Random.insideUnitCircle.normalized;
                
                // B. Get Random Distance (Between Min and Max)
                float rndDist = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                
                // C. Calculate Candidate Position
                Vector3 candidate = origin + new Vector3(rndDir.x, 0, rndDir.y) * rndDist;

                // D. Snap to NavMesh
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    // E. Double Check Distance
                    // Sometimes SamplePosition snaps the point BACK towards the agent.
                    // We verify the final point is still far enough away.
                    if (Vector3.Distance(origin, hit.position) >= config.minPatrolDistance)
                    {
                        return hit.position;
                    }
                }
            }
            return null;
        }
    }
}