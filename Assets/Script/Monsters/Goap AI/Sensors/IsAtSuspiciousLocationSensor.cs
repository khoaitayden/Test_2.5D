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

            // 1. Logic Override: If we have points, we are "At Location" (doing the job)
            if (coverFinder != null && coverFinder.HasPoints) return 1;

            // 2. Physical Distance Check
            // Flatten Y to handle elevation differences
            Vector3 current = agent.Transform.position; current.y = 0;
            Vector3 target = brain.LastKnownPlayerPosition; target.y = 0;
            
            float dist = Vector3.Distance(current, target);
            
            // Must match the threshold in InvestigateTargetSensor
            float threshold = config.stoppingDistance + 1.0f;

            return (dist <= threshold) ? 1 : 0;
        }
    }
}