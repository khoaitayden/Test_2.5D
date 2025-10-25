using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();

            // SET RELAXED PATROL SPEED
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.Patrol);

            stuckDetector.StartTracking(agent.Transform.position);
            
            Debug.Log($"[Patrol] Starting patrol to {data.Target?.Position}");
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null)
            {
                Debug.LogWarning("[Patrol] No target available!");
                return ActionRunState.Stop;
            }

            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                Debug.LogWarning("[Patrol] Monster is STUCK! Requesting new patrol point.");
                return ActionRunState.Stop;
            }

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f)
            {
                Debug.Log("[Patrol] ✓ Reached patrol point!");
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