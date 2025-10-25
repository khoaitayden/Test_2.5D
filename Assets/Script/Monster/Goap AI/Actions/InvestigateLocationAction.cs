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
            SearchingPhase1,  // Tight search, faster
            SearchingPhase2,  // Expanded search, slower
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
            data.investigationStartTime = Time.time;
            data.currentSearchRadius = config.investigateStartRadius;
            data.hasExpandedSearch = false;

            // SET FAST RUSH SPEED - we're heading to where we lost them!
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
            float investigationDuration = Time.time - data.investigationStartTime;

            // Check timeout
            if (investigationDuration > config.maxInvestigationTime)
            {
                Debug.Log($"[Investigate] Giving up after {investigationDuration:F1}s. Returning to patrol speed.");
                return ActionRunState.Completed;
            }

            // DO NOT abort if player spotted - let the brain handle the goal change
            // The action will be stopped by GOAP when KillPlayerGoal is requested
            // Just stop checking for player here to avoid conflicts

            // Handle progressive search expansion
            if (!data.hasExpandedSearch && investigationDuration > config.expandSearchAfter)
            {
                data.hasExpandedSearch = true;
                data.currentSearchRadius = config.investigateMaxRadius;
                Debug.Log($"[Investigate] ⚠️ EXPANDING SEARCH! New radius: {data.currentSearchRadius}m");
            }

            // Gradually slow down during search phases
            if (data.state == InvestigateState.SearchingPhase1 || data.state == InvestigateState.SearchingPhase2)
            {
                MonsterSpeedController.UpdateInvestigationSpeed(navMeshAgent, config, investigationDuration);
            }

            if (data.minMoveTime > 0f)
                data.minMoveTime -= context.DeltaTime;

            bool hasArrived = data.minMoveTime <= 0f &&
                             !navMeshAgent.pathPending &&
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            // STATE 1: RUSHING to last seen position (FAST)
            if (data.state == InvestigateState.GoingToLastSeenPosition)
            {
                if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
                {
                    Debug.LogWarning("[Investigate] STUCK going to last seen position. Aborting.");
                    return ActionRunState.Stop;
                }

                if (hasArrived)
                {
                    Debug.Log("[Investigate] Arrived! Starting PHASE 1 search (tight, fast)...");
                    
                    // Switch to search speed
                    MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateSearch);
                    
                    data.state = InvestigateState.SearchingPhase1;
                    data.lookPoints = GenerateSearchPoints(agent, config.investigateStartRadius, config.phase1Points);

                    if (!MoveToNextLookPoint(agent, data))
                    {
                        Debug.Log("[Investigate] No valid look points in Phase 1. Completing.");
                        return ActionRunState.Completed;
                    }
                }
            }

            // STATE 2: PHASE 1 SEARCH (tight radius, still relatively fast)
            else if (data.state == InvestigateState.SearchingPhase1)
            {
                if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
                {
                    Debug.LogWarning("[Investigate] STUCK in Phase 1. Trying next point.");
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        // Phase 1 exhausted, move to Phase 2 if expanded
                        if (data.hasExpandedSearch)
                        {
                            TransitionToPhase2(agent, data);
                        }
                        else
                        {
                            return ActionRunState.Completed;
                        }
                    }
                }

                if (hasArrived)
                {
                    StartRotating(agent, data);
                }

                // Check if we should transition to Phase 2
                if (data.hasExpandedSearch && data.lookPoints.Count == 0)
                {
                    TransitionToPhase2(agent, data);
                }
            }

            // STATE 3: PHASE 2 SEARCH (expanded radius, slower, more cautious)
            else if (data.state == InvestigateState.SearchingPhase2)
            {
                if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
                {
                    Debug.LogWarning("[Investigate] STUCK in Phase 2. Trying next point.");
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        Debug.Log("[Investigate] Phase 2 exhausted. Giving up.");
                        return ActionRunState.Completed;
                    }
                }

                if (hasArrived)
                {
                    StartRotating(agent, data);
                }
            }

            // STATE 4: ROTATING at point (stationary)
            else if (data.state == InvestigateState.RotatingAtPoint)
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
                        // Done rotating, move to next point
                        if (!MoveToNextLookPoint(agent, data))
                        {
                            // Check if we need to transition phases
                            if (data.state == InvestigateState.SearchingPhase1 && data.hasExpandedSearch)
                            {
                                TransitionToPhase2(agent, data);
                            }
                            else
                            {
                                Debug.Log("[Investigate] ========== INVESTIGATION COMPLETE ==========");
                                return ActionRunState.Completed;
                            }
                        }
                    }
                }
            }

            return ActionRunState.Continue;
        }

        private void TransitionToPhase2(IMonoAgent agent, Data data)
        {
            Debug.Log("[Investigate] === PHASE 2: EXPANDED SEARCH (slower, wider area) ===");
            data.state = InvestigateState.SearchingPhase2;
            data.lookPoints = GenerateSearchPoints(agent, data.currentSearchRadius, config.phase2Points);

            if (!MoveToNextLookPoint(agent, data))
            {
                Debug.Log("[Investigate] No valid points in Phase 2. Completing.");
                // Will be completed on next cycle
            }
        }

        private void StartRotating(IMonoAgent agent, Data data)
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
            int attemptsUsed = 0;

            while (points.Count < pointCount && attemptsUsed < maxAttempts)
            {
                attemptsUsed++;
                Vector3 randomPoint = searchCenter + Random.insideUnitSphere * radius;
                randomPoint.y = searchCenter.y;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius * 2f, NavMesh.AllAreas))
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

            Debug.Log($"[Investigate] Generated {points.Count}/{pointCount} points (radius: {radius}m)");
            return points;
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
                return false;

            Vector3 nextPoint = data.lookPoints.Dequeue();
            
            // Return to appropriate search state
            if (data.state == InvestigateState.RotatingAtPoint)
            {
                // Determine which phase we're in based on what we were doing before rotation
                if (data.hasExpandedSearch && data.currentSearchRadius >= config.investigateMaxRadius * 0.9f)
                {
                    data.state = InvestigateState.SearchingPhase2;
                }
                else
                {
                    data.state = InvestigateState.SearchingPhase1;
                }
            }
            
            data.minMoveTime = 0.5f;

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(nextPoint);
            stuckDetector.StartTracking(agent.Transform.position);

            Debug.Log($"[Investigate] Moving to next point ({data.lookPoints.Count} left)");
            return true;
        }

    public override void End(IMonoAgent agent, Data data)
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();

        stuckDetector.Reset();
        bool isPlayerVisibleNow = PlayerInSightSensor.IsPlayerInSight(agent, config);

        if (isPlayerVisibleNow)
        {
            Debug.Log("[Investigate] Investigation INTERRUPTED by player sighting. Notifying brain is unnecessary.");
            return; // Exit the method early.
        }

        
        var brain = agent.GetComponent<MonsterBrain>();
        if (brain != null)
            brain.OnInvestigationComplete();

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

            // Rotation data
            public float rotationAngle;
            public float rotationSpeed;
            public float rotatedAmount;
            public int rotationDirection;
        }
    }
}