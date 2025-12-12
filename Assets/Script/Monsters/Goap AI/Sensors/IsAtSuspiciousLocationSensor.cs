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
        private MonsterConfig config;
        private CoverFinder coverFinder;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>();
            
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero) return 0;

            // 1. Logic Override: If we are already searching (queue has points), we are "Here".
            if (coverFinder != null && coverFinder.HasPoints) return 1;

            // 2. Physical Distance Check
            Vector3 current = agent.Transform.position; current.y = 0;
            Vector3 target = brain.LastKnownPlayerPosition; target.y = 0;
            
            float dist = Vector3.Distance(current, target);

            // FIX: Use 'investigateRadius' instead of 'stoppingDistance'.
            // If we are anywhere inside the zone, stop trying to "Go To Center" 
            // and switch to "Search Cover Points".
            float threshold = config.investigateRadius; 

            return (dist <= threshold) ? 1 : 0;
        }
    }
}