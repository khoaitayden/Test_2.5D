using CrashKonijn.Agent.Core;
using CrashKonijn.Docs.GettingStarted.Behaviours;
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
            RotatingAtPoint // ← New state for scanning rotation
        }

        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private MonsterMoveBehaviour moveBehaviour;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (moveBehaviour == null) moveBehaviour = agent.GetComponent<MonsterMoveBehaviour>();

            // #### TAKE CONTROL ####
            if (moveBehaviour != null)
                moveBehaviour.enabled = false;

            data.state = InvestigateState.GoingToLastSeenPosition;
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;
            data.minMoveTime = 0.5f;

            if (data.Target != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(data.Target.Position);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Prevent early arrival check
            if (data.minMoveTime > 0f)
                data.minMoveTime -= context.DeltaTime;

            bool hasArrived = data.minMoveTime <= 0f &&
                              !navMeshAgent.pathPending &&
                              navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            // STATE 1: Go to the last seen position
            if (data.state == InvestigateState.GoingToLastSeenPosition)
            {
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
                else if (CheckIfStuck(agent, data, context))
                {
                    Debug.LogWarning("[Investigate] STUCK going to last seen position. Aborting.");
                    return ActionRunState.Stop;
                }
            }

            // STATE 2: Moving between look points
            else if (data.state == InvestigateState.LookingAround)
            {
                if (hasArrived)
                {
                    // Stop and start rotating instead of waiting
                    data.state = InvestigateState.RotatingAtPoint;
                    data.rotationSpeed = Random.Range(90f, 120f);
                    data.rotationAngle = Random.Range(90f, 200f);
                    data.rotationDirection = 1;
                    data.rotatedAmount = 0f;
                    data.startRotation = agent.Transform.rotation;
                    navMeshAgent.isStopped = true;

                    Debug.Log($"[Investigate] Arrived at look point. Scanning area for {data.rotationAngle:F0}°");
                }
                else if (CheckIfStuck(agent, data, context))
                {
                    Debug.LogWarning("[Investigate] STUCK while moving to look point. Trying next point.");
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        Debug.Log("[Investigate] No more look points after getting stuck. Completing.");
                        return ActionRunState.Completed;
                    }
                }
            }

            // STATE 3: Rotating (scanning) at point
            else if (data.state == InvestigateState.RotatingAtPoint)
            {
                float rotateStep = data.rotationSpeed * context.DeltaTime * data.rotationDirection;
                agent.Transform.Rotate(0f, rotateStep, 0f);
                data.rotatedAmount += Mathf.Abs(rotateStep);

                // Once full angle rotated → reverse direction once
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
                        // Done scanning both ways, move to next look point
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

        // ==== Helper Functions ====

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
                        Debug.Log($"[Investigate] Generated valid look point #{points.Count}");
                    }
                }
            }

            Debug.Log($"[Investigate] Generated {points.Count} look points.");
            return points;
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
                return false;

            Vector3 nextPoint = data.lookPoints.Dequeue();
            data.state = InvestigateState.LookingAround;
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;
            data.minMoveTime = 0.5f;

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(nextPoint);

            Debug.Log($"[Investigate] Moving to next look point. ({data.lookPoints.Count} left)");
            return true;
        }

        private bool CheckIfStuck(IMonoAgent agent, Data data, IActionContext context)
        {
            float distanceMoved = Vector3.Distance(agent.Transform.position, data.lastPosition);
            float movementThreshold = config.stuckVelocityThreshold * context.DeltaTime;

            if (distanceMoved < movementThreshold)
                data.stuckTimer += context.DeltaTime;
            else
            {
                data.lastPosition = agent.Transform.position;
                data.stuckTimer = 0f;
            }

            if (data.stuckTimer > config.maxStuckTime)
            {
                Debug.LogWarning("[Investigate] Stuck detection triggered!");
                return true;
            }

            return false;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();

            if (moveBehaviour != null)
                moveBehaviour.enabled = true;

            var brain = agent.GetComponent<MonsterBrain>();
            if (brain != null)
                brain.OnInvestigationComplete();
        }

        // ==== Data Class ====
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public Vector3 lastPosition;
            public float stuckTimer;
            public float minMoveTime;
            public InvestigateState state;
            public Queue<Vector3> lookPoints;

            // Rotation data
            public float rotationAngle;
            public float rotationSpeed;
            public float rotatedAmount;
            public int rotationDirection;
            public Quaternion startRotation;
        }
    }
}
