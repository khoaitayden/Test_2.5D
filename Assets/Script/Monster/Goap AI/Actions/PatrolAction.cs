// FILE TO EDIT: PatrolAction.cs (UPGRADED)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        private NavMeshAgent navMeshAgent;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();

            if (data.Target != null)
            {
                navMeshAgent.SetDestination(data.Target.Position);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}