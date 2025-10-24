using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateLocationAction : GoapActionBase<InvestigateLocationAction.Data>
    {
        public enum InvestigateState { GoingToLastSeenPosition, LookingAround, WaitingAtPoint }
        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();

            data.state = InvestigateState.GoingToLastSeenPosition;
            data.waitTimer = 0f;
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;
            data.minMoveTime = 0f;

            if (data.Target != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(data.Target.Position);
                Debug.Log("[Investigate] Starting investigation, heading to last seen position.");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Update minimum move time (prevents premature arrival detection)
            if (data.minMoveTime > 0f)
            {
                data.minMoveTime -= context.DeltaTime;
            }

            bool hasArrived = data.minMoveTime <= 0f &&
                             !navMeshAgent.pathPending && 
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            // STATE 1: Going to the last seen position
            if (data.state == InvestigateState.GoingToLastSeenPosition)
            {
                if (hasArrived)
                {
                    Debug.Log("[Investigate] Arrived at last seen position. Generating look points.");
                    data.state = InvestigateState.LookingAround;
                    data.lookPoints = GenerateReachableLookPoints(agent, config);
                    
                    // Move to first point
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        // No valid points, complete immediately
                        Debug.Log("[Investigate] No valid look points. Completing investigation.");
                        return ActionRunState.Completed;
                    }
                }
                else
                {
                    // Check for stuck while moving to last seen position
                    if (CheckIfStuck(agent, data, context))
                    {
                        Debug.LogWarning("[Investigate] STUCK going to last seen position. Aborting.");
                        return ActionRunState.Stop;
                    }
                }
            }
            // STATE 2: Looking around at various points
            else if (data.state == InvestigateState.LookingAround)
            {
                if (hasArrived)
                {
                    // We've reached a look point, now wait a moment
                    data.state = InvestigateState.WaitingAtPoint;
                    data.waitTimer = Random.Range(0.1f, 0.5f);
                    navMeshAgent.isStopped = true; // Stop moving while waiting
                    Debug.Log($"[Investigate] Arrived at look point. Waiting {data.waitTimer:F2}s. ({data.lookPoints.Count} remaining)");
                }
                else
                {
                    // Check for stuck while moving between look points
                    if (CheckIfStuck(agent, data, context))
                    {
                        Debug.LogWarning("[Investigate] STUCK while moving to look point. Trying next point.");
                        // Instead of stopping, try the next point
                        if (!MoveToNextLookPoint(agent, data))
                        {
                            Debug.Log("[Investigate] No more look points after getting stuck. Completing.");
                            return ActionRunState.Completed;
                        }
                    }
                }
            }
            // STATE 3: Waiting at a look point before moving to the next
            else if (data.state == InvestigateState.WaitingAtPoint)
            {
                data.waitTimer -= context.DeltaTime;
                
                if (data.waitTimer <= 0f)
                {
                    // Done waiting, move to next point or finish
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        Debug.Log("[Investigate] ========== FINISHED ALL LOOK POINTS ==========");
                        Debug.Log("[Investigate] Returning ActionRunState.Completed - This should trigger HasInvestigated effect!");
                        return ActionRunState.Completed;
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
            int maxAttempts = pointsToGenerate * 5; // Try harder to find valid points
            int attemptsUsed = 0;
            
            while (points.Count < pointsToGenerate && attemptsUsed < maxAttempts)
            {
                attemptsUsed++;
                Vector3 randomPoint = searchCenter + Random.insideUnitSphere * config.investigateRadius;
                randomPoint.y = searchCenter.y; // Keep on same Y level
                
                // Find nearest point on NavMesh
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, config.investigateRadius * 2f, NavMesh.AllAreas))
                {
                    // Skip if too close to current position
                    if (Vector3.Distance(hit.position, searchCenter) < 2f)
                        continue;
                        
                    // CRITICAL: Verify the point is actually reachable
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(agent.Transform.position, hit.position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            points.Enqueue(hit.position);
                            Debug.Log($"[Investigate] Generated valid look point #{points.Count} at distance {Vector3.Distance(searchCenter, hit.position):F1}m");
                        }
                    }
                }
            }
            
            Debug.Log($"[Investigate] Generated {points.Count} reachable look points (wanted {pointsToGenerate}, tried {attemptsUsed} times)");
            return points;
        }
        
        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
            {
                return false;
            }
            
            Vector3 nextPoint = data.lookPoints.Dequeue();
            
            // Reset movement tracking
            data.state = InvestigateState.LookingAround;
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;
            data.minMoveTime = 0.5f; // Give agent 0.5s to start moving before checking arrival
            
            // Actually command the NavMeshAgent to move
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(nextPoint);
            
            Debug.Log($"[Investigate] Moving to look point at {nextPoint}. ({data.lookPoints.Count} remaining). Distance: {Vector3.Distance(agent.Transform.position, nextPoint):F1}m");
            return true;
        }
        
        private bool CheckIfStuck(IMonoAgent agent, Data data, IActionContext context)
        {
            float distanceMoved = Vector3.Distance(agent.Transform.position, data.lastPosition);
            float movementThreshold = config.stuckVelocityThreshold * context.DeltaTime;
            
            if (distanceMoved < movementThreshold)
            {
                data.stuckTimer += context.DeltaTime;
            }
            else
            {
                data.lastPosition = agent.Transform.position;
                data.stuckTimer = 0f;
            }
            
            if (data.stuckTimer > config.maxStuckTime)
            {
                Debug.LogWarning($"[Investigate] Stuck detection triggered! Stuck for {data.stuckTimer:F2}s, moved only {distanceMoved:F3}m");
                return true;
            }
            
            return false;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();
                
            // Find the brain and tell it we are done.
            var brain = agent.GetComponent<MonsterBrain>();
            if (brain != null)
            {
                brain.OnInvestigationComplete();
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public Vector3 lastPosition;
            public float stuckTimer;
            public float waitTimer;
            public float minMoveTime;
            public InvestigateState state;
            public Queue<Vector3> lookPoints;
        }
    }
}