using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerCurrentPosSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        private MonsterConfig config;

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();

            // We need to add 'playerAnchor' to MonsterConfig first! (See below)
            if (config.playerAnchor != null && config.playerAnchor.Value != null)
            {
                return new TransformTarget(config.playerAnchor.Value);
            }

            // Fallback
            return new PositionTarget(agent.Transform.position);
        }
    }
}