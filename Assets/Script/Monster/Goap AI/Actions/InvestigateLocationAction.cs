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
            SearchingPhase1,
            SearchingPhase2,
            RotatingAtPoint
        }

        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();

            data.state = InvestigateState.GoingToLastSeenPosition;
            data.minMoveTime = 0.5f;
            data.investigationStartTime = Time.time;
            data.currentSearchRadius = config.investigateStartRadius;
            data.hasExpandedSearch = false;

            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateRush);
            
            if (data.Target != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(data.Target.Position);
                stuckDetector.StartTracking(agent.Transform.position);
                Debug.Log("[Investigate] RUSHING to last seen position!");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            float duration = Time.time - data.investigationStartTime;

            // Timeout check
            if (duration > config.maxInvestigationTime)
            {
                Debug.Log($"[Investigate] Giving up after {duration:F1}s.");
                return ActionRunState.Completed;
            }

            // Progressive search expansion
            if (!data.hasExpandedSearch && duration > config.expandSearchAfter)
            {
                data.hasExpandedSearch = true;
                data.currentSearchRadius = config.investigateMaxRadius;
                Debug.Log($"[Investigate] EXPANDING SEARCH! New radius: {data.currentSearchRadius}m");
            }

            // Update speed during search phases
            if (IsSearching(data.state))
                MonsterSpeedController.UpdateInvestigationSpeed(navMeshAgent, config, duration);

            data.minMoveTime = Mathf.Max(0f, data.minMoveTime - context.DeltaTime);
            bool hasArrived = data.minMoveTime <= 0f && !navMeshAgent.pathPending && 
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            // Handle different states
            switch (data.state)
            {
                case InvestigateState.GoingToLastSeenPosition:
                    return HandleRushingToLastSeenPlayerLocation(agent, data, hasArrived);

                case InvestigateState.SearchingPhase1:
                case InvestigateState.SearchingPhase2:
                    return HandleSearching(agent, data, context, hasArrived);

                case InvestigateState.RotatingAtPoint:
                    return HandleRotating(agent, data, context);
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleRushingToLastSeenPlayerLocation(IMonoAgent agent, Data data, bool hasArrived)
        {
            if (stuckDetector.CheckStuck(agent.Transform.position, Time.deltaTime, config))
            {
                Debug.LogWarning("[Investigate] STUCK going to last seen position. Aborting.");
                return ActionRunState.Stop;
            }

            if (hasArrived)
            {
                Debug.Log("[Investigate] Arrived! Starting PHASE 1 search (tight, fast)...");
                MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateSearch);
                
                data.state = InvestigateState.SearchingPhase1;
                data.lookPoints = GenerateSearchPoints(agent, config.investigateStartRadius, config.phase1Points);

                return MoveToNextLookPoint(agent, data) ? ActionRunState.Continue : ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleSearching(IMonoAgent agent, Data data, IActionContext context, bool hasArrived)
        {
            bool isPhase1 = data.state == InvestigateState.SearchingPhase1;

            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                Debug.LogWarning($"[Investigate] STUCK in {(isPhase1 ? "Phase 1" : "Phase 2")}. Trying next point.");
                
                if (!MoveToNextLookPoint(agent, data))
                    return HandlePhaseCompletion(agent, data, isPhase1);
            }

            if (hasArrived)
                StartRotating(data);

            // Phase transition check
            if (isPhase1 && data.hasExpandedSearch && data.lookPoints.Count == 0)
                TransitionToPhase2(agent, data);

            return ActionRunState.Continue;
        }

        private IActionRunState HandleRotating(IMonoAgent agent, Data data, IActionContext context)
        {
            float rotateStep = data.rotationSpeed * context.DeltaTime * data.rotationDirection;
            agent.Transform.Rotate(0f, rotateStep, 0f);
            data.rotatedAmount += Mathf.Abs(rotateStep);

            if (data.rotatedAmount >= data.rotationAngle)
            {
                if (data.rotationDirection == 1)
                {
                    // Rotate back
                    data.rotationDirection = -1;
                    data.rotatedAmount = 0f;
                }
                else
                {
                    // Done rotating, move to next point
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        bool wasPhase1 = data.currentSearchRadius < config.investigateMaxRadius * 0.9f;
                        return HandlePhaseCompletion(agent, data, wasPhase1);
                    }
                }
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandlePhaseCompletion(IMonoAgent agent, Data data, bool wasPhase1)
        {
            if (wasPhase1 && data.hasExpandedSearch)
            {
                TransitionToPhase2(agent, data);
                return ActionRunState.Continue;
            }

            Debug.Log("[Investigate] ========== INVESTIGATION COMPLETE ==========");
            return ActionRunState.Completed;
        }

        private void TransitionToPhase2(IMonoAgent agent, Data data)
        {
            Debug.Log("[Investigate] === PHASE 2: EXPANDED SEARCH (slower, wider area) ===");
            data.state = InvestigateState.SearchingPhase2;
            data.lookPoints = GenerateSearchPoints(agent, data.currentSearchRadius, config.phase2Points);

            if (!MoveToNextLookPoint(agent, data))
                Debug.Log("[Investigate] No valid points in Phase 2. Completing.");
        }

        private void StartRotating(Data data)
        {
            data.state = InvestigateState.RotatingAtPoint;
            data.rotationSpeed = Random.Range(90f, 120f);
            data.rotationAngle = Random.Range(90f, 180f);
            data.rotationDirection = 1;
            data.rotatedAmount = 0f;
            navMeshAgent.isStopped = true;
            stuckDetector.Reset();

            Debug.Log($"[Investigate] Scanning area ({data.rotationAngle:F0}°)");
        }

        private Queue<Vector3> GenerateSearchPoints(IMonoAgent agent, float radius, int pointCount)
        {
            var points = new Queue<Vector3>();
            Vector3 searchCenter = agent.Transform.position;
            int maxAttempts = pointCount * 5;

            for (int i = 0; i < maxAttempts && points.Count < pointCount; i++)
            {
                Vector3 randomPoint = searchCenter + Random.insideUnitSphere * radius;
                randomPoint.y = searchCenter.y;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius * 2f, NavMesh.AllAreas) &&
                    Vector3.Distance(hit.position, searchCenter) >= 2f)
                {
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(agent.Transform.position, hit.position, NavMesh.AllAreas, path) &&
                        path.status == NavMeshPathStatus.PathComplete)
                    {
                        points.Enqueue(hit.position);
                    }
                }
            }

            Debug.Log($"[Investigate] Generated {points.Count}/{pointCount} points (radius: {radius}m)");
            return points;
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
                return false;

            Vector3 nextPoint = data.lookPoints.Dequeue();
            
            // Restore appropriate search state after rotation
            if (data.state == InvestigateState.RotatingAtPoint)
            {
                data.state = (data.hasExpandedSearch && data.currentSearchRadius >= config.investigateMaxRadius * 0.9f)
                    ? InvestigateState.SearchingPhase2
                    : InvestigateState.SearchingPhase1;
            }
            
            data.minMoveTime = 0.5f;
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(nextPoint);
            stuckDetector.StartTracking(agent.Transform.position);

            Debug.Log($"[Investigate] Moving to next point ({data.lookPoints.Count} left)");
            return true;
        }

        private bool IsSearching(InvestigateState state)
        {
            return state == InvestigateState.SearchingPhase1 || state == InvestigateState.SearchingPhase2;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();

            stuckDetector.Reset();

            if (PlayerInSightSensor.IsPlayerInSight(agent, config))
            {
                Debug.Log("[Investigate] Investigation INTERRUPTED by player sighting.");
                return;
            }

            var brain = agent.GetComponent<MonsterBrain>();
            brain?.OnInvestigationComplete();

            Debug.Log("[Investigate] Investigation ended naturally. Signalling return to patrol.");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float minMoveTime;
            public float investigationStartTime;
            public float currentSearchRadius;
            public bool hasExpandedSearch;
            public InvestigateState state;
            public Queue<Vector3> lookPoints;
            public float rotationAngle;
            public float rotationSpeed;
            public float rotatedAmount;
            public int rotationDirection;
        }
    }
}