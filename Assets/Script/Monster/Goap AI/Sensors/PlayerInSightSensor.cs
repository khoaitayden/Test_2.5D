// FILE TO EDIT: PlayerInSightSensor.cs (Corrected Version)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerInSightSensor : LocalWorldSensorBase
    {
        private MonsterConfig config;
        
        // This is the default Sense method for the GOAP runner.
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            // Now this call is valid because the types match perfectly.
            return IsPlayerInSight(agent, references.GetCachedComponent<MonsterConfig>());
        }

        // #### THE ONLY CHANGE IS ON THIS LINE ####
        // We now accept the more general 'IActionReceiver', which works for both calls.
        public static bool IsPlayerInSight(IActionReceiver agent, MonsterConfig config)
        {
            if (config == null) return false;

            // 'IActionReceiver' also has a .Transform property, so the rest of the logic is unchanged.
            Vector3 eyesPosition = agent.Transform.position + Vector3.up * 0.5f;

            var colliders = new Collider[10];
            var count = Physics.OverlapSphereNonAlloc(
                eyesPosition,
                config.ViewRadius,
                colliders,
                config.PlayerLayerMask
            );

            if (count == 0) return false;

            for (int i = 0; i < count; i++)
            {
                Transform target = colliders[i].transform;
                Vector3 directionToTarget = (target.position - eyesPosition).normalized;
                
                if (Vector3.Angle(agent.Transform.forward, directionToTarget) < config.ViewAngle / 2)
                {
                    float distanceToTarget = Vector3.Distance(eyesPosition, target.position);
                    Debug.DrawRay(eyesPosition, directionToTarget * distanceToTarget, Color.red);
                    
                    if (!Physics.Raycast(eyesPosition, directionToTarget, distanceToTarget, config.ObstacleLayerMask))
                    {
                        // Removed the debug log from here to reduce spam. The brain's log is better.
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Created() { }
        public override void Update() { }
    }
}