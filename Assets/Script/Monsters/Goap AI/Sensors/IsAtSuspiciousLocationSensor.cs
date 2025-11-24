using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsAtSuspiciousLocationSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;
        private MonsterMovement movement;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (movement == null) movement = references.GetCachedComponent<MonsterMovement>();

            if (brain == null || movement == null) return 0;
            
            Vector3 target = brain.LastKnownPlayerPosition;
            if (target == Vector3.zero) return 0;

            // USE THE SHARED LOGIC
            // Note: Sensor uses 'Investigate' mode logic implicitly
            bool arrived = movement.HasReached(target);

            return arrived ? 1 : 0;
        }
    }
}