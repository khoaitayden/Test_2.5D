using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class HasSuspiciousLocationSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;

        public override void Created() { }
        public override void Update() { }

        // The sensor's job is to sense the monster's internal memory, which is managed by the brain.
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            // Get a reference to the brain if we don't have one.
            if (this.brain == null)
                this.brain = references.GetCachedComponent<MonsterBrain>();

            if (this.brain == null)
                return 0; // If there's no brain, there's no suspicious location.

            // The "fact" is true IF the brain has stored a valid last known position.
            // We check if it's not the default Vector3.zero.
            bool hasLocation = this.brain.LastKnownPlayerPosition != Vector3.zero;

            // Return 1 if true, 0 if false.
            return hasLocation ? 1 : 0;
        }
    }
}