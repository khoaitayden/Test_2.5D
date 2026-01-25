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
        private DrunkMonsterConfig config;
        private CoverFinder coverFinder;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (config == null) config = references.GetCachedComponent<DrunkMonsterConfig>();
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>();
            
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero) return 0;

            if (coverFinder != null && coverFinder.HasPoints) return 1;

            Vector3 current = agent.Transform.position; current.y = 0;
            Vector3 target = brain.LastKnownPlayerPosition; target.y = 0;
            
            float dist = Vector3.Distance(current, target);

            float threshold = config.investigateRadius; 

            return (dist <= threshold) ? 1 : 0;
        }
    }
}