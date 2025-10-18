using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerInSightSensor : LocalWorldSensorBase
    {
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (config == null)
                config = references.GetCachedComponent<MonsterConfig>();

            if (config == null)
                return false;

            var colliders = new Collider[10];
            var count = Physics.OverlapSphereNonAlloc(
                agent.Transform.position, 
                config.ViewRadius, 
                colliders, 
                config.PlayerLayerMask
            );

            if (count == 0)
                return false;

            // CHANGED: Iterate through found colliders and check the angle.
            for (int i = 0; i < count; i++)
            {
                var playerCollider = colliders[i];
                if (playerCollider == null) continue;

                Vector3 directionToPlayer = (playerCollider.transform.position - agent.Transform.position).normalized;

                // Check if the player is within the view cone.
                if (Vector3.Angle(agent.Transform.forward, directionToPlayer) < config.ViewAngle / 2)
                {
                    // Player is in sight and within the cone!
                    Debug.Log($"[PlayerInSightSensor] PLAYER DETECTED IN CONE!");
                    return true;
                }
            }

            // No player was found within the cone.
            return false;
        }
    }
}