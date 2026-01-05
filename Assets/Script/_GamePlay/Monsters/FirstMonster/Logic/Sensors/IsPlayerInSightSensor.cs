using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsPlayerInSightSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            // 1. Get the Brain (The Single Source of Truth)
            var brain = references.GetCachedComponent<MonsterBrain>();

            // 2. Safety Check
            if (brain == null) return 0;
            
            // 3. Read Memory (0 Cost)
            // We rely 100% on MonsterVision updating this value in the Brain.
            return brain.IsPlayerVisible ? 1 : 0;
        }
    }
}