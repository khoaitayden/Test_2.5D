using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateLocationAction : GoapActionBase<InvestigateLocationAction.Data>
    {
        public enum InvestigateState
        {
            GoingToLastSeenPosition,
            LookingAround,
            RotatingAtPoint
        }

        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();

            data.state = InvestigateState.GoingToLastSeenPosition;
            data.minMoveTime = 0.5f;

            if (data.Target != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(data.Target.Position);
                stuckDetector.StartTracking(agent.Transform.position);
                Debug.Log("[Investigate] Starting investigation, heading to last seen position.");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Early exit if player spotted
            if (PlayerInSightSensor.IsPlayerInSight(agent, config))
            {
                Debug.LogWarning("[Investigate] Player spotted during investigation! Aborting.");
                return ActionRunState.Stop;
            }

            // Prevent early arrival check
            if (data.minMoveTime > 0f)
                data.minMoveTime -= context.DeltaTime;

            bool hasArrived = data.minMoveTime <= 0f &&
                             !navMeshAgent.pathPending &&
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            // STATE 1: Going to last seen position
            if (data.state == InvestigateState.GoingToLastSeenPosition)
            {
                // Check for stuck
                if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
                {
                    Debug.LogWarning("[Investigate] STUCK going to last seen position. Aborting investigation.");
                    return ActionRunState.Stop;
                }

                if (hasArrived)
                {
                    Debug.Log("[Investigate] Arrived at last seen position. Generating look points.");
                    data.state = InvestigateState.LookingAround;
                    data.lookPoints = GenerateReachableLookPoints(agent, config);

                    if (!MoveToNextLookPoint(agent, data))
                    {
                        Debug.Log("[Investigate] No valid look points. Completing investigation.");
                        return ActionRunState.Completed;
                    }
                }
            }

            // STATE 2: Moving between look points
            else if (data.state == InvestigateState.LookingAround)
            {
                // Check for stuck
                if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
                {
                    Debug.LogWarning("[Investigate] STUCK while moving to look point. Trying next point.");
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        Debug.Log("[Investigate] No more look points after getting stuck. Completing.");
                        return ActionRunState.Completed;
                    }
                }

                if (hasArrived)
                {
                    // Stop and start rotating
                    data.state = InvestigateState.RotatingAtPoint;
                    data.rotationSpeed = Random.Range(90f, 120f);
                    data.rotationAngle = Random.Range(90f, 180f);
                    data.rotationDirection = 1;
                    data.rotatedAmount = 0f;
                    navMeshAgent.isStopped = true;
                    stuckDetector.Reset(); // Not moving, so don't check for stuck

                    Debug.Log($"[Investigate] Arrived at look point. Scanning {data.rotationAngle:F0}°");
                }
            }

            // STATE 3: Rotating at point
            else if (data.state == InvestigateState.RotatingAtPoint)
            {
                float rotateStep = data.rotationSpeed * context.DeltaTime * data.rotationDirection;
                agent.Transform.Rotate(0f, rotateStep, 0f);
                data.rotatedAmount += Mathf.Abs(rotateStep);

                if (data.rotatedAmount >= data.rotationAngle)
                {
                    if (data.rotationDirection == 1)
                    {
                        // Reverse once
                        data.rotationDirection = -1;
                        data.rotatedAmount = 0f;
                        Debug.Log("[Investigate] Scanning in reverse direction.");
                    }
                    else
                    {
                        // Done scanning, move to next point
                        if (!MoveToNextLookPoint(agent, data))
                        {
                            Debug.Log("[Investigate] ========== FINISHED ALL LOOK POINTS ==========");
                            return ActionRunState.Completed;
                        }
                    }
                }
            }

            return ActionRunState.Continue;
        }

        private Queue<Vector3> GenerateReachableLookPoints(IMonoAgent agent, MonsterConfig config)
        {
            var points = new Queue<Vector3>();
            Vector3 searchCenter = agent.Transform.position;
            int pointsToGenerate = Random.Range(config.minInvestigatePoints, config.maxInvestigatePoints + 1);
            int maxAttempts = pointsToGenerate * 5;
            int attemptsUsed = 0;

            while (points.Count < pointsToGenerate && attemptsUsed < maxAttempts)
            {
                attemptsUsed++;
                Vector3 randomPoint = searchCenter + Random.insideUnitSphere * config.investigateRadius;
                randomPoint.y = searchCenter.y;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, config.investigateRadius * 2f, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(hit.position, searchCenter) < 2f)
                        continue;

                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(agent.Transform.position, hit.position, NavMesh.AllAreas, path) &&
                        path.status == NavMeshPathStatus.PathComplete)
                    {
                        points.Enqueue(hit.position);
                    }
                }
            }

            Debug.Log($"[Investigate] Generated {points.Count} reachable look points.");
            return points;
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
                return false;

            Vector3 nextPoint = data.lookPoints.Dequeue();
            data.state = InvestigateState.LookingAround;
            data.minMoveTime = 0.5f;

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(nextPoint);
            
            // Restart stuck detection for this new destination
            stuckDetector.StartTracking(agent.Transform.position);

            Debug.Log($"[Investigate] Moving to next look point. ({data.lookPoints.Count} left)");
            return true;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();

            stuckDetector.Reset();

            var brain = agent.GetComponent<MonsterBrain>();
            if (brain != null)
                brain.OnInvestigationComplete();

            Debug.Log("[Investigate] Investigation action ended.");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float minMoveTime;
            public InvestigateState state;
            public Queue<Vector3> lookPoints;

            // Rotation data
            public float rotationAngle;
            public float rotationSpeed;
            public float rotatedAmount;
            public int rotationDirection;
        }
    }
}