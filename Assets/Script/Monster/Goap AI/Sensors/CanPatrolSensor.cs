// FILE TO EDIT: CanPatrolSensor.cs

using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class CanPatrolSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            // Cache components for efficiency.
            if (brain == null)
                brain = references.GetCachedComponent<MonsterBrain>();
            
            if (config == null)
                config = references.GetCachedComponent<MonsterConfig>();

            if (brain == null || config == null)
            {
                return 0;
            }

            bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(agent, config);
            bool hasSuspiciousLocation = brain.LastKnownPlayerPosition != UnityEngine.Vector3.zero;

            if (isPlayerVisible || hasSuspiciousLocation)
            {
                // There is something more important to do. Do NOT patrol.
                Debug.Log("Patrol false");
                return 0;
            }
            Debug.Log("Patrol true");
            // There are no threats or clues. The monster is free to patrol.
            return 1;
        }
    }
}