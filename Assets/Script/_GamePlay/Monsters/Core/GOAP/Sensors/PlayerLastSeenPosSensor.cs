using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerLastSeenPosSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

         public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            var brain = references.GetCachedComponent<MonsterBrain>();
            
            if (brain == null) return null;

            Vector3 lastPos = brain.LastKnownPlayerPosition;

            // Valid check
            if (lastPos == Vector3.zero) return null;

            return new PositionTarget(lastPos);
        }
    }
}