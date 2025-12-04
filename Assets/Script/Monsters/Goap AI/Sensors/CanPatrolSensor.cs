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

            // --- THE FIX ---
            // Instead of calculating vision again, just read the Brain's memory.
            bool isPlayerVisible = brain.IsPlayerVisible;
            
            // Logic: Can only patrol if NOT fighting and NOT investigating.
            bool isBusy = isPlayerVisible || brain.IsInvestigating;

            return isBusy ? 0 : 1;
        }
    }
}