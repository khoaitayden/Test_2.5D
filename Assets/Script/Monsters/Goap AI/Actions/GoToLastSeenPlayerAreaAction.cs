using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerAreaAction : GoapActionBase<GoToLastSeenPlayerAreaAction.Data>
    {
        private MonsterMovement movement;
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            if (brain == null) brain = agent.GetComponent<MonsterBrain>();

            data.actionFailed = false;

            if (data.Target != null)
            {
                movement.GoTo(data.Target.Position, MonsterMovement.SpeedState.Investigate);
            }
            else
            {
                data.actionFailed = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.actionFailed) return ActionRunState.Stop;
            if (movement.IsStuck) return ActionRunState.Stop; // Fail if stuck going to investigate

            // USE SHARED ARRIVAL LOGIC
            if (movement.HasReached(data.Target.Position))
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            if (data.actionFailed)
                brain?.OnInvestigationFailed();
            else
                brain?.OnArrivedAtSuspiciousLocation();
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public bool actionFailed;
        }
    }
}