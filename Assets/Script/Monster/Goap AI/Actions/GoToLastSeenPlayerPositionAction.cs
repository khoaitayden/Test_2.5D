using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerPositionAction : GoapActionBase<GoToLastSeenPlayerPositionAction.Data>
    {
        private NavMeshAgent navMeshAgent;

        public override void Created() { }
        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = agent.GetComponent<NavMeshAgent>();
            }
        }
        public override void End(IMonoAgent agent, Data data) { }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null)
                return ActionRunState.Stop;

            float distance = Vector3.Distance(agent.Transform.position, data.Target.Position);

            if (distance < navMeshAgent.stoppingDistance + 0.5f)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}