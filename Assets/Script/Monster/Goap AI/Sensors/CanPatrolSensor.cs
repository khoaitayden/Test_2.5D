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
            if (brain == null)
                brain = references.GetCachedComponent<MonsterBrain>();
            
            if (config == null)
                config = references.GetCachedComponent<MonsterConfig>();

            if (brain == null || config == null)
                return 0;

            // Fact 1: Is the player visible?
            bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(agent, config);

            // Fact 2: Are we in the middle of an investigation?
            bool isInvestigating = brain.IsInvestigating;

            // We can only patrol if both of these are false.
            if (isPlayerVisible || isInvestigating)
            {
                Debug.Log("no patrol pls");
                return 0;
            }
            Debug.Log("yes patrol pls");
            return 1;
        }
    }
}