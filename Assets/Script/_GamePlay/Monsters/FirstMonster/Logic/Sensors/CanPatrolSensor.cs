using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class CanPatrolSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            var brain = references.GetCachedComponent<MonsterBrain>();
            if (brain == null) return 0;

            // Simple Logic: 
            // If we are actively Chasing (PlayerVisible) or Searching Area (Investigating), don't patrol.
            // If we just hear a noise, we COULD patrol, but the planner will prefer the Noise Action because it's cheaper.
            bool busy = brain.IsPlayerVisible || brain.IsInvestigating;

            return busy ? 0 : 1;
        }
    }
}