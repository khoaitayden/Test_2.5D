using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities; // Important for MonsterMovement
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

            if (data.Target == null)
            {
                data.actionFailed = true;
                return;
            }

            // Command the Unified Movement System
            bool success = movement.GoTo(data.Target.Position, MonsterMovement.SpeedState.Investigate);

            if (!success)
            {
                Debug.LogWarning($"[GoTo] Cannot find path to {data.Target.Position}. Marking failure.");
                data.actionFailed = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.actionFailed) return ActionRunState.Stop;

            // Simplified: The movement component handles the 'how', we just check the status
            if (movement.IsStuck)
            {
                data.actionFailed = true;
                return ActionRunState.Stop;
            }

            if (movement.HasArrived)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (data.actionFailed)
            {
                movement.Stop();
                brain?.OnInvestigationFailed();
            }
            else
            {
                // We assume successful arrival if not failed
                brain?.OnArrivedAtSuspiciousLocation();
            }
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public bool actionFailed;
        }
    }
}