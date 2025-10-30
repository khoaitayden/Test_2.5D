using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPositionAction : GoapActionBase<GoToLastSeenPositionAction.Data>
    {
        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();

            // Set the speed for rushing to the location.
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateRush);

            if (data.Target != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(data.Target.Position);
                stuckDetector.StartTracking(agent.Transform.position);
                Debug.Log("[GoTo] RUSHING to last seen position!");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Abort if we get stuck.
            if (stuckDetector.CheckStuck(agent.Transform.position, Time.deltaTime, config))
            {
                Debug.LogWarning("[GoTo] STUCK going to last seen position. Aborting.");
                return ActionRunState.Stop;
            }

            // Check if we have arrived.
            bool hasArrived = !navMeshAgent.pathPending &&
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            if (hasArrived)
            {
                Debug.Log("[GoTo] Arrived at last seen position.");
                // This action's job is complete.
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            stuckDetector.Reset();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}