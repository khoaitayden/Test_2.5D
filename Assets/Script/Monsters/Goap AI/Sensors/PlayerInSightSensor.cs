using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerInSightSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null)
                brain = references.GetCachedComponent<MonsterBrain>();

            if (brain == null) return 0;
            
            // NO RAYCASTS. Just read memory.
            return brain.IsPlayerVisible ? 1 : 0;
        }

        // Keep this static logic for the Vision System to use, 
        // OR move the logic into MonsterVision.cs entirely. 
        // For now, keeping it here so you don't break existing calls, 
        // but remember: GOAP doesn't call this anymore. MonsterVision calls this.
        public static bool IsPlayerInSight(IActionReceiver agent, MonsterConfig config)
        {
             if (config == null) return false;

            Vector3 eyesPosition = agent.Transform.position + Vector3.up * 0.5f;

            // Reuse array in real implementation to avoid GC alloc
            var colliders = new Collider[10]; 
            int count = Physics.OverlapSphereNonAlloc(
                eyesPosition,
                config.viewRadius,
                colliders,
                config.playerLayerMask
            );

            if (count == 0) return false;

            for (int i = 0; i < count; i++)
            {
                Transform target = colliders[i].transform;
                Vector3 toTarget = target.position - eyesPosition;
                float distanceToTarget = toTarget.magnitude;

                if (distanceToTarget > config.viewRadius) continue;

                if (Vector3.Angle(agent.Transform.forward, toTarget) > config.ViewAngle / 2f)
                    continue;

                // Simple single Check for robust vision
                if (!Physics.Raycast(eyesPosition, toTarget.normalized, distanceToTarget, config.obstacleLayerMask))
                {
                    return true;
                }
            }

            return false;
        }
    }
}