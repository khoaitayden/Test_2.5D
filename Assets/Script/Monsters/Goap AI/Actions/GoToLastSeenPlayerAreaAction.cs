using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerAreaAction : GoapActionBase<GoToLastSeenPlayerAreaAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();
            
            if (data.Target != null)
            {
                // Simple Move Call
                movement.MoveTo(data.Target.Position, config.investigateSpeed, config.stoppingDistance);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;
            
            if (movement.HasArrivedOrStuck())
            {
                brain?.OnArrivedAtSuspiciousLocation();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data) { movement.Stop(); }
        public class Data : IActionData { public ITarget Target { get; set; } }
    }
}