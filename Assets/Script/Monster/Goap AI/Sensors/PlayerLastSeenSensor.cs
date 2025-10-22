// FILE TO EDIT: PlayerLastSeenSensor.cs (Corrected)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerLastSeenSensor : LocalTargetSensorBase
    {
        private MonsterBrain monsterBrain;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (monsterBrain == null)
            {
                // #### THIS IS THE FIX ####
                // We get the Transform from the interface, and then GetComponent from the Transform.
                monsterBrain = agent.Transform.GetComponent<MonsterBrain>();
            }
                
            if (monsterBrain == null)
                return null;

            // This logic remains correct.
            if (monsterBrain.LastKnownPlayerPosition != Vector3.zero)
            {
                return new PositionTarget(monsterBrain.LastKnownPlayerPosition);
            }
            
            return null;
        }
    }
}