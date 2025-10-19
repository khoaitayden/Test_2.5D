// FILE TO EDIT: AttackPlayerAction.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        // Cache the touch sensor for performance
        private MonsterTouchSensor touchSensor;

        public override void Created() { }
        public override void Start(IMonoAgent agent, Data data) { }
        public override void End(IMonoAgent agent, Data data) { }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Cache the component on the first run
            if (touchSensor == null)
                touchSensor = agent.GetComponent<MonsterTouchSensor>();

            if (data.Target == null || touchSensor == null)
                return ActionRunState.Stop;

            // NEW LOGIC: Check the boolean from our sensor script
            if (touchSensor.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                return ActionRunState.Completed;
            }

            // While we are not touching, the action continues. The movement behaviour will keep moving the agent closer.
            return ActionRunState.Continue;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}