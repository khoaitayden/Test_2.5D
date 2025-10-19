// FILE TO EDIT: PlayerSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime; // Required for TransformTarget
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

            var colliders = new Collider[1];
            var count = Physics.OverlapSphereNonAlloc(
                agent.Transform.position,
                config.ViewRadius,
                colliders,
                config.PlayerLayerMask
            );

            if (count == 0)
                return null;

            // THE KEY CHANGE IS HERE!
            // We now return a TransformTarget, which tracks the player's transform dynamically.
            return new TransformTarget(colliders[0].transform);
        }
    }
}