using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        public override void Created()
        {
        }

        public override void Start(IMonoAgent agent, Data data)
        {
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // CORRECTED LINES: Get components from the 'agent' parameter, not the 'context'.
            var config = agent.GetComponent<PatrolConfig>();
            var navMeshAgent = agent.GetComponent<NavMeshAgent>();

            if (config == null || navMeshAgent == null || data.Target == null)
            {
                Debug.LogError("PatrolAction is missing a required component (PatrolConfig, NavMeshAgent) or Target is null.");
                return ActionRunState.Stop;
            }

            // --- Unstuck Logic ---
            float distanceMoved = Vector3.Distance(agent.Transform.position, data.lastPosition);
            if (distanceMoved < config.StuckDistanceThreshold)
            {
                data.stuckTimer += context.DeltaTime;
            }
            else
            {
                data.stuckTimer = 0f;
                data.lastPosition = agent.Transform.position;
            }

            if (data.stuckTimer > config.MaxStuckTime)
            {
                Debug.LogWarning($"Agent is stuck at {agent.Transform.position}. Finding a new patrol point.");
                return ActionRunState.Stop;
            }
            
            // --- Completion Logic ---
            float distanceToTarget = Vector3.Distance(agent.Transform.position, data.Target.Position);
            if (distanceToTarget <= navMeshAgent.stoppingDistance)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public Vector3 lastPosition;
            public float stuckTimer;
        }
    }
}