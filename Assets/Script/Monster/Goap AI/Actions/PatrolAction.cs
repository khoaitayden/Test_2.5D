// FILE TO REPLACE: PatrolAction.cs (The Final, Correct Version)
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
        public override void Start(IMonoAgent agent, Data data) { } // Start is empty!

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null)
            {
                // This can happen if the sensor fails to find a target.
                return ActionRunState.Stop;
            }
            
            if (navMeshAgent == null)
                navMeshAgent = agent.GetComponent<NavMeshAgent>();

            // The only job is to check if we've arrived. MonsterMoveBehaviour is doing the moving.
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data) { }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}