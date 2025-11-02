using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerAreaAction : GoapActionBase<GoToLastSeenPlayerAreaAction.Data>
    {
        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private MonsterBrain brain; // Store a reference to the brain

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();
            brain ??= agent.GetComponent<MonsterBrain>(); // Get the brain

            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateRush);
            Debug.Log($"[GoTo] RUSHING to last seen position. Movement controlled by MonsterMoveBehaviour.");
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Continue;

            float distanceToTarget = Vector3.Distance(agent.Transform.position, data.Target.Position);
            bool hasArrived = distanceToTarget <= navMeshAgent.stoppingDistance;

            if (hasArrived)
            {
                Debug.Log("[GoTo] Arrived at delayed last seen position.");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            Debug.Log("Go to last seen area end");
            // The action's job is to report its completion to the brain.
            // The brain will decide what this means for the world state.
            brain?.OnArrivedAtSuspiciousLocation();
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}