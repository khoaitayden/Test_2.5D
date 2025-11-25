using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsAtSuspiciousLocationSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero) return 0;

            Vector3 current = agent.Transform.position; current.y = 0;
            Vector3 target = brain.LastKnownPlayerPosition; target.y = 0;
            
            float dist = Vector3.Distance(current, target);

            // FIX: Use the new unified variable 'baseStoppingDistance'.
            // We add a small buffer (+1.0f) so the Sensor is slightly "easier" 
            // to satisfy than the physical movement, preventing the planner from 
            // fighting the physics engine.
            float required = config.baseStoppingDistance + 1.0f;

            return (dist <= required) ? 1 : 0;
        }
    }
}