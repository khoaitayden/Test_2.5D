// FILE TO EDIT: InvestigateLocationAction.cs (Disable MoveBehaviour Fix)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Docs.GettingStarted.Behaviours;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateLocationAction : GoapActionBase<InvestigateLocationAction.Data>
    {
        public enum InvestigateState { GoToLocation, LookAround }
        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private MonsterMoveBehaviour moveBehaviour;
        private Vector3 initialTargetPosition;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (moveBehaviour == null) moveBehaviour = agent.GetComponent<MonsterMoveBehaviour>();

            // CRITICAL: Disable MonsterMoveBehaviour so it doesn't fight for control
            if (moveBehaviour != null)
            {
                moveBehaviour.enabled = false;
                Debug.Log("[InvestigateLocationAction] Disabled MonsterMoveBehaviour");
            }

            data.state = InvestigateState.GoToLocation;
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;
            data.gracePeriod = 0.5f;
            data.hasSetInitialDestination = false;

            if (data.Target != null)
            {
                initialTargetPosition = data.Target.Position;
                Debug.Log($"[InvestigateLocationAction] Target position stored: {initialTargetPosition}");
            }
            else
            {
                Debug.LogError("[InvestigateLocationAction] No target provided!");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            Debug.Log($"[InvestigateLocationAction] Perform called! State: {data.state}, HasSetDestination: {data.hasSetInitialDestination}");
            
            if (!data.hasSetInitialDestination)
            {
                navMeshAgent.SetDestination(initialTargetPosition);
                navMeshAgent.isStopped = false; // Ensure agent is moving
                data.hasSetInitialDestination = true;
                Debug.Log($"[InvestigateLocationAction] Moving to last seen position: {initialTargetPosition}");
            }

            if (data.gracePeriod > 0f)
                data.gracePeriod -= context.DeltaTime;

            switch (data.state)
            {
                case InvestigateState.GoToLocation:
                    if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                    {
                        Debug.Log("[InvestigateLocationAction] Arrived. Beginning to look around.");
                        data.state = InvestigateState.LookAround;
                        
                        data.lookPoints = new Queue<Vector3>();
                        int pointsToGenerate = Random.Range(config.minInvestigatePoints, config.maxInvestigatePoints + 1);
                        Debug.Log($"[InvestigateLocationAction] Generating {pointsToGenerate} look points");
                        
                        for (int i = 0; i < pointsToGenerate; i++)
                        {
                            Vector3 searchCenter = agent.Transform.position;
                            Vector3 randomPoint = searchCenter + Random.insideUnitSphere * config.investigateRadius;
                            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, config.investigateRadius, NavMesh.AllAreas))
                                data.lookPoints.Enqueue(hit.position);
                        }

                        Debug.Log($"[InvestigateLocationAction] Successfully generated {data.lookPoints.Count} look points");

                        if (data.lookPoints.Count > 0)
                        {
                            Vector3 nextPoint = data.lookPoints.Dequeue();
                            navMeshAgent.SetDestination(nextPoint);
                            navMeshAgent.isStopped = false;
                            Debug.Log($"[InvestigateLocationAction] Moving to look point: {nextPoint}");
                            
                            data.gracePeriod = 0.5f;
                            data.stuckTimer = 0f;
                            data.lastPosition = agent.Transform.position;
                        }
                        else
                        {
                            Debug.LogWarning("[InvestigateLocationAction] No valid look points generated, completing action");
                            return ActionRunState.Completed;
                        }
                    }
                    break;

                case InvestigateState.LookAround:
                    if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                    {
                        if (data.lookPoints.Count == 0)
                        {
                            Debug.Log("[InvestigateLocationAction] Finished looking around. Goal complete.");
                            return ActionRunState.Completed;
                        }
                        
                        Vector3 nextPoint = data.lookPoints.Dequeue();
                        navMeshAgent.SetDestination(nextPoint);
                        navMeshAgent.isStopped = false;
                        Debug.Log($"[InvestigateLocationAction] Moving to next look point: {nextPoint} ({data.lookPoints.Count} remaining)");
                        
                        data.gracePeriod = 0.5f;
                        data.stuckTimer = 0f;
                        data.lastPosition = agent.Transform.position;
                    }
                    break;
            }

            // Stuck detection only after grace period
            if (data.gracePeriod <= 0f && navMeshAgent.hasPath && !navMeshAgent.pathPending)
            {
                if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                {
                    float distanceMoved = Vector3.Distance(agent.Transform.position, data.lastPosition);
                    
                    if (distanceMoved < config.stuckVelocityThreshold)
                    {
                        data.stuckTimer += context.DeltaTime;
                        
                        if (data.stuckTimer > config.maxStuckTime * 0.5f)
                        {
                            Debug.LogWarning($"[InvestigateLocationAction] Agent moving slowly. Stuck timer: {data.stuckTimer:F2}/{config.maxStuckTime:F2}, Distance moved: {distanceMoved:F3}");
                        }
                    }
                    else
                    {
                        data.stuckTimer = 0f;
                        data.lastPosition = agent.Transform.position;
                    }

                    if (data.stuckTimer > config.maxStuckTime)
                    {
                        Debug.LogWarning($"[InvestigateLocationAction] Agent is STUCK. Position: {agent.Transform.position}, Destination: {navMeshAgent.destination}, Remaining: {navMeshAgent.remainingDistance}");
                        return ActionRunState.Stop;
                    }
                }
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            // Re-enable MonsterMoveBehaviour when done
            if (moveBehaviour != null)
            {
                moveBehaviour.enabled = true;
                Debug.Log("[InvestigateLocationAction] Re-enabled MonsterMoveBehaviour");
            }

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();
                
            Debug.Log("[InvestigateLocationAction] Action ended");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; } // Keep for interface compliance but unused
            public Vector3 lastPosition;
            public float stuckTimer;
            public float gracePeriod;
            public InvestigateState state;
            public Queue<Vector3> lookPoints;
            public bool hasSetInitialDestination;
        }
    }
}