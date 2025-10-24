// FILE TO REPLACE: AttackPlayerAction.cs (The Final, Correct Version)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        private MonsterTouchSensor touchSensor;

        public override void Created() { }
        public override void Start(IMonoAgent agent, Data data){} // Start is empty!

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null)
            {
                // The Player sensor lost the target.
                return ActionRunState.Stop;
            }
            
            if (touchSensor == null) 
                touchSensor = agent.GetComponent<MonsterTouchSensor>();

            // The only job is to check if we've touched the player. MonsterMoveBehaviour is doing the chasing.
            if (touchSensor != null && touchSensor.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                return ActionRunState.Completed;
            }
            
            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data) { } // End is empty!

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}