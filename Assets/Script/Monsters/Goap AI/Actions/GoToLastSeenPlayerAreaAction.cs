// FILE TO EDIT: GoToLastSeenPlayerAreaAction.cs

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
        private MonsterBrain brain;
        private StuckDetector stuckDetector = new StuckDetector(); // We need this back

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();
            brain ??= agent.GetComponent<MonsterBrain>();
            stuckDetector.Reset();

            data.actionFailed = false;

            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateRush);
            
            if (data.Target != null)
            {
                // --- ROBUSTNESS CHECK 1: Is the target reachable? ---
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(agent.Transform.position, data.Target.Position, NavMesh.AllAreas, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    // Path is valid, proceed.
                    navMeshAgent.SetPath(path);
                    stuckDetector.StartTracking(agent.Transform.position);
                    Debug.Log($"[GoTo] RUSHING to last seen position. Movement controlled by MonsterMoveBehaviour.");
                }
                else
                {
                    // The point is unreachable from our current position.
                    Debug.LogWarning($"[GoTo] Last known player position {data.Target.Position} is UNREACHABLE. Aborting investigation.");
                    data.actionFailed = true; // Flag the action as failed.
                }
            }
            else
            {
                Debug.LogWarning("[GoTo] Action started with no target. Aborting investigation.");
                data.actionFailed = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // If the action failed on start, tell the planner to stop.
            if (data.actionFailed)
            {
                return ActionRunState.Stop;
            }
            
            // --- ROBUSTNESS CHECK 2: Are we stuck while moving? ---
            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                Debug.LogWarning("[GoTo] Got STUCK while moving to last known position. Aborting investigation.");
                data.actionFailed = true; // Flag the failure.
                return ActionRunState.Stop;
            }

            if (data.Target == null) return ActionRunState.Continue;

            float distanceToTarget = Vector3.Distance(agent.Transform.position, data.Target.Position);
            bool hasArrived = !navMeshAgent.pathPending && 
                             distanceToTarget <= navMeshAgent.stoppingDistance;

            if (hasArrived)
            {
                Debug.Log("[GoTo] Arrived at last seen position.");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            Debug.Log("Go to last seen area end");
            stuckDetector.Reset();

            if (data.actionFailed)
            {
                // If the action failed, notify the brain so it can clean up and go to patrol.
                brain?.OnInvestigationFailed();
            }
            else
            {
                // Otherwise, the action succeeded. Report arrival so the Search action can start.
                brain?.OnArrivedAtSuspiciousLocation();
            }
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public bool actionFailed; // Flag to track failure state.
        }
    }
}