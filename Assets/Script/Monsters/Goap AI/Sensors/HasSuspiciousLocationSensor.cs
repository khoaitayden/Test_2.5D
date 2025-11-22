using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class HasSuspiciousLocationSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            var brain = references.GetCachedComponent<MonsterBrain>();

            if (brain == null) return 0;

            // Just check if the vector is not (0,0,0)
            // If logic: Memory exists? -> 1 (True)
            return brain.LastKnownPlayerPosition != Vector3.zero ? 1 : 0;
        }
    }
}