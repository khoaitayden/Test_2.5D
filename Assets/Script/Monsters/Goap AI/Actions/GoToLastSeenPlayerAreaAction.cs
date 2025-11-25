using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerAreaAction : GoapActionBase<GoToLastSeenPlayerAreaAction.Data>
    {
        private MonsterMovement movement;
        
        public override void Created() { }
        public override void Start(IMonoAgent agent, Data data)
        {
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            
            if (data.Target != null)
                movement.GoTo(data.Target.Position, MonsterMovement.SpeedState.Investigate);
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Simple single check
            if (movement.HasReachedDestination())
                return ActionRunState.Completed;

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            // Does NOT notify brain here. Wait for SearchAction to start.
        }
        public class Data : IActionData { public ITarget Target { get; set; } }
    }
}