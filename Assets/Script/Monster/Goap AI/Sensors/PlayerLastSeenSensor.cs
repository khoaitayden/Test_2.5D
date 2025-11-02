// FILE TO EDIT: PlayerLastSeenSensor.cs

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
                monsterBrain = agent.Transform.GetComponent<MonsterBrain>();
            }
                
            if (monsterBrain == null)
                return null;

            // --- THIS IS THE FIX ---
            // Instead of using the old LastKnownPlayerPosition, we now use the same
            // public property that the MonsterBrain updates after its delay.
            // This ensures both GoTo and Search actions use the same, intelligent target.
            Vector3 targetPosition = monsterBrain.LastKnownPlayerPosition;

            // Only return a target if the brain has actually set a valid position.
            if (targetPosition != Vector3.zero)
            {
                return new PositionTarget(targetPosition);
            }
            
            return null;
        }
    }
}