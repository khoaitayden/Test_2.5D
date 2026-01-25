using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerCurrentPosSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        private MonsterBrain brain;

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();

            if (brain != null && brain.PlayerAnchor != null && brain.PlayerAnchor.Value != null)
            {
                return new TransformTarget(brain.PlayerAnchor.Value);
            }

            return new PositionTarget(agent.Transform.position);
        }
    }
}