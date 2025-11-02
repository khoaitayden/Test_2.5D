// FILE TO EDIT: IsInvestigatingSensor.cs

using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsInvestigatingSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null)
                brain = references.GetCachedComponent<MonsterBrain>();

            if (brain == null)
                return 0;

            // Read the public property from the brain.
            return brain.IsInvestigating ? 1 : 0;
        }
    }
}