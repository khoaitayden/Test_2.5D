using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        // This is the correct override for Created() in an Action
        public override void Created() { }
        
        public override void Start(IMonoAgent agent, Data data)
        {
            // CHANGED: Get the brain and reset the touch state at the start of the action.
            data.brain = agent.GetComponent<MonsterBrain>();
            if (data.brain != null)
            {
                data.brain.ResetTouchState();
            }
        }
        
        public override void End(IMonoAgent agent, Data data) { }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // CHANGED: The entire logic of this action.
            if (data.Target == null || data.brain == null)
                return ActionRunState.Stop;

            // Check the flag from the MonsterBrain.
            if (data.brain.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                return ActionRunState.Completed;
            }

            // Keep chasing until the trigger fires.
            return ActionRunState.Continue;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            // ADDED: A reference to the brain to check the touch state.
            public MonsterBrain brain;
        }
    }
}