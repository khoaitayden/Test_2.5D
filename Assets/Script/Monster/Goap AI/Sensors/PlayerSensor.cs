using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null)
                config = references.GetCachedComponent<MonsterConfig>();

            if (config == null) return null;

            var colliders = new Collider[10]; // Increased buffer just in case
            var count = Physics.OverlapSphereNonAlloc(
                agent.Transform.position, 
                config.ViewRadius, 
                colliders, 
                config.PlayerLayerMask
            );

            if (count == 0)
                return null;
                
            // CHANGED: Iterate through found colliders and check the angle.
            for (int i = 0; i < count; i++)
            {
                var playerCollider = colliders[i];
                if (playerCollider == null) continue;
                
                Vector3 directionToPlayer = (playerCollider.transform.position - agent.Transform.position).normalized;

                // Check if the player is within the view cone.
                if (Vector3.Angle(agent.Transform.forward, directionToPlayer) < config.ViewAngle / 2)
                {
                    // Player is in the cone, return their position as the target.
                    return new PositionTarget(playerCollider.transform.position);
                }
            }
            
            // No player found in the cone, return no target.
            return null;
        }
    }
}