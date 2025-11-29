using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsAtSuspiciousLocationSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero) return 0;

            // Physical Distance Check
            float dist = Vector3.Distance(agent.Transform.position, brain.LastKnownPlayerPosition);

            // Use the Investigate Radius itself.
            // If we are ANYWHERE inside the investigation zone, we are "At Location".
            // This allows us to move between cover points without triggering "GoTo".
            return (dist <= config.investigateRadius) ? 1 : 0;
        }
    }
}