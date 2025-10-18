// In Assets/Monster/Goap AI/Sensors/PlayerSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    // This is the ONLY sensor for the player. It does both jobs.
    public class PlayerSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;

        // LocalTargetSensorBase requires these overrides.
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            // Cache the config on the first run.
            if (config == null)
                config = references.GetCachedComponent<MonsterConfig>();

            if (config == null) return null;

            var colliders = new Collider[1];
            var count = Physics.OverlapSphereNonAlloc(agent.Transform.position, config.ViewRadius, colliders, config.PlayerLayerMask);

            // Get the GoapActionProvider, which holds the world state.
            var provider = references.GetCachedComponent<GoapActionProvider>();
            
            if (count == 0)
            {
                // Set the world state using the correct syntax. Use 0 for false.
                provider.WorldData.SetState(new PlayerInSight(), 0);
                // Return null because no target was found.
                return null;
            }
            
            // Set the world state using the correct syntax. Use 1 for true.
            provider.WorldData.SetState(new PlayerInSight(), 1);
            // Return the player's position as the target.
            return new PositionTarget(colliders[0].transform.position);
        }
    }
}