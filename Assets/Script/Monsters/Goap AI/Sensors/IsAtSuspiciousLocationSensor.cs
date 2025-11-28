using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities; // Needed for CoverFinder
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
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>(); // Cache CoverFinder
            
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero) return 0;

            // --- CONDITION 1: LOGICAL OVERRIDE ---
            // If the CoverFinder has points in the queue, we are technically "On The Job".
            // We consider the location "reached & active". 
            // This prevents the planner from forcing us back to the center point while moving between cover points.
            if (coverFinder != null && coverFinder.HasPoints)
            {
                return 1;
            }

            // --- CONDITION 2: PHYSICAL DISTANCE ---
            // Only fall back to distance check if queue is empty (e.g. initial arrival)
            Vector3 current = agent.Transform.position; current.y = 0;
            Vector3 target = brain.LastKnownPlayerPosition; target.y = 0;
            
            float dist = Vector3.Distance(current, target);

            // Using stopping distance + buffer
            float threshold = config.baseStoppingDistance + 1.0f;

            return (dist <= threshold) ? 1 : 0;
        }
    }
}