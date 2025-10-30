using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class SearchSurroundingsAction : GoapActionBase<SearchSurroundingsAction.Data>
    {
        public enum SearchState { SearchingPhase1, SearchingPhase2, RotatingAtPoint }

        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();

            data.investigationStartTime = Time.time;
            data.currentSearchRadius = config.investigateStartRadius;
            data.hasExpandedSearch = false;
            
            Debug.Log("[Search] Starting PHASE 1 search (tight, fast)...");
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateSearch);
            
            data.state = SearchState.SearchingPhase1;
            data.lookPoints = GenerateSearchPoints(agent, config.investigateStartRadius, config.phase1Points);

            MoveToNextLookPoint(agent, data);
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            float duration = Time.time - data.investigationStartTime;

            if (duration > config.maxInvestigationTime)
            {
                Debug.Log($"[Search] Giving up after {duration:F1}s.");
                return ActionRunState.Completed;
            }

            if (!data.hasExpandedSearch && duration > config.expandSearchAfter)
            {
                data.hasExpandedSearch = true;
                data.currentSearchRadius = config.investigateMaxRadius;
                Debug.Log($"[Search] EXPANDING SEARCH! New radius: {data.currentSearchRadius}m");
            }

            MonsterSpeedController.UpdateInvestigationSpeed(navMeshAgent, config, duration);

            bool hasArrived = !navMeshAgent.pathPending &&
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            switch (data.state)
            {
                case SearchState.SearchingPhase1:
                case SearchState.SearchingPhase2:
                    return HandleSearching(agent, data, context, hasArrived);
                case SearchState.RotatingAtPoint:
                    return HandleRotating(agent, data, context);
            }

            return ActionRunState.Continue;
        }
        
        private IActionRunState HandleSearching(IMonoAgent agent, Data data, IActionContext context, bool hasArrived)
        {
            bool isPhase1 = data.state == SearchState.SearchingPhase1;

            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                Debug.LogWarning($"[Search] STUCK in {(isPhase1 ? "Phase 1":"Phase 2")}. Trying next point.");
                if (!MoveToNextLookPoint(agent, data)) return HandlePhaseCompletion(agent, data, isPhase1);
            }

            if (hasArrived) StartRotating(data);
            
            if (isPhase1 && data.hasExpandedSearch && data.lookPoints.Count == 0) TransitionToPhase2(agent, data);

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
                    data.rotationDirection = -1;
                    data.rotatedAmount = 0f;
                }
                else
                {
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

            Debug.Log("[Search] ========== SEARCH COMPLETE ==========");
            return ActionRunState.Completed;
        }

        private void TransitionToPhase2(IMonoAgent agent, Data data)
        {
            Debug.Log("[Search] === PHASE 2: EXPANDED SEARCH (slower, wider area) ===");
            data.state = SearchState.SearchingPhase2;
            data.lookPoints = GenerateSearchPoints(agent, data.currentSearchRadius, config.phase2Points);

            if (!MoveToNextLookPoint(agent, data)) Debug.Log("[Search] No valid points in Phase 2. Completing.");
        }

        private void StartRotating(Data data)
        {
            data.state = SearchState.RotatingAtPoint;
            data.rotationSpeed = Random.Range(90f, 120f);
            data.rotationAngle = Random.Range(90f, 180f);
            data.rotationDirection = 1;
            data.rotatedAmount = 0f;
            navMeshAgent.isStopped = true;
            stuckDetector.Reset();
            Debug.Log($"[Search] Scanning area ({data.rotationAngle:F0}°)");
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
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius * 2f, NavMesh.AllAreas) && Vector3.Distance(hit.position, searchCenter) >= 2f)
                {
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(agent.Transform.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                        points.Enqueue(hit.position);
                }
            }
            Debug.Log($"[Search] Generated {points.Count}/{pointCount} points (radius: {radius}m)");
            return points;
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0) return false;
            
            Vector3 nextPoint = data.lookPoints.Dequeue();
            
            if (data.state == SearchState.RotatingAtPoint)
            {
                data.state = (data.hasExpandedSearch && data.currentSearchRadius >= config.investigateMaxRadius * 0.9f) ? SearchState.SearchingPhase2 : SearchState.SearchingPhase1;
            }
            
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(nextPoint);
            stuckDetector.StartTracking(agent.Transform.position);
            Debug.Log($"[Search] Moving to next point ({data.lookPoints.Count} left)");
            return true;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
            stuckDetector.Reset();

            // This is important: clear the intermediate state when we're done.
            var provider = agent.GetComponent<GoapActionProvider>();
            if (provider != null)
                provider.WorldData.SetState(new IsAtSuspiciousLocation(), 0);

            var brain = agent.GetComponent<MonsterBrain>();
            brain?.OnInvestigationComplete();
        }

        public class Data : IActionData
        {
            // #### THIS IS THE FIX ####
            // Adding this property back in, even though we don't use it,
            // allows the GOAP planner to correctly validate the action.
            public ITarget Target { get; set; }

            public float investigationStartTime, currentSearchRadius, rotationAngle, rotationSpeed, rotatedAmount;
            public bool hasExpandedSearch;
            public SearchState state;
            public Queue<Vector3> lookPoints;
            public int rotationDirection;
        }
    }
}