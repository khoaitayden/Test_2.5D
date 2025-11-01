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
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();

            data.hasAttemptedRepath = false;

            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateRush);

            if (data.Target != null)
            {
                // Instead of going directly to last seen position, calculate an area point
                Vector3 areaDestination = CalculateSearchAreaPoint(agent, data.Target.Position);
                
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(areaDestination);
                stuckDetector.StartTracking(agent.Transform.position);
                
                Debug.Log($"[GoTo] RUSHING toward area near last seen position! (Offset: {Vector3.Distance(data.Target.Position, areaDestination):F1}m)");
            }
            else
            {
                Debug.LogWarning("[GoTo] Action started with no target. Stopping.");
                data.actionFailed = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.actionFailed)
            {
                return ActionRunState.Stop;
            }

            // Check if we are stuck
            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                if (data.hasAttemptedRepath)
                {
                    Debug.LogWarning("[GoTo] STUCK again after repathing. Aborting investigation.");
                    return ActionRunState.Stop;
                }
                
                if (TryRepathNearby(agent, data))
                {
                    Debug.Log("[GoTo] STUCK! Trying a new path to a nearby point.");
                    data.hasAttemptedRepath = true;
                    stuckDetector.Reset();
                    stuckDetector.StartTracking(agent.Transform.position);
                }
                else
                {
                    Debug.LogWarning("[GoTo] STUCK and could not find a valid nearby point. Aborting.");
                    return ActionRunState.Stop;
                }
            }

            // Check if we have arrived
            bool hasArrived = !navMeshAgent.pathPending &&
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            if (hasArrived)
            {
                Debug.Log("[GoTo] Arrived in the general area of last seen position.");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            stuckDetector.Reset();
        }

        /// <summary>
        /// Calculates a point in the general area of the last seen position.
        /// The monster overshoots or goes to a nearby point to simulate intelligent pursuit.
        /// </summary>
        private Vector3 CalculateSearchAreaPoint(IMonoAgent agent, Vector3 lastSeenPosition)
        {
            Vector3 monsterPos = agent.Transform.position;
            Vector3 directionToTarget = (lastSeenPosition - monsterPos).normalized;
            
            // Calculate an overshoot distance - monster runs past where player was seen
            float overshootDistance = Random.Range(config.overshootMinDistance, config.overshootMaxDistance);
            
            // Add some randomness to the direction (player might have turned)
            float randomAngle = Random.Range(-config.searchAreaAngleVariance, config.searchAreaAngleVariance);
            Vector3 randomizedDirection = Quaternion.Euler(0, randomAngle, 0) * directionToTarget;
            
            // Calculate the target point (past the last seen position)
            Vector3 targetPoint = lastSeenPosition + randomizedDirection * overshootDistance;
            
            // Find a valid point on the NavMesh
            if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, config.overshootMaxDistance * 2f, NavMesh.AllAreas))
            {
                // Verify we can actually path there
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(monsterPos, hit.position, NavMesh.AllAreas, path) && 
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
            
            // Fallback: If overshoot point is invalid, just go to last seen position
            Debug.LogWarning("[GoTo] Overshoot point invalid, using last seen position directly.");
            return lastSeenPosition;
        }

        private bool TryRepathNearby(IMonoAgent agent, Data data)
        {
            if (data.Target == null) return false;

            Vector3 originalTarget = data.Target.Position;
            float searchRadius = 5f;

            if (NavMesh.SamplePosition(originalTarget, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(agent.Transform.position, hit.position, NavMesh.AllAreas, path) && 
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    navMeshAgent.SetDestination(hit.position);
                    return true;
                }
            }

            return false;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public bool hasAttemptedRepath;
            public bool actionFailed;
        }
    }
}