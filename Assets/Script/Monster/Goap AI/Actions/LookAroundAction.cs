// FILE TO EDIT: LookAroundAction.cs (FIXED and UPGRADED)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class LookAroundAction : GoapActionBase<LookAroundAction.Data>
    {
        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache components for performance
            if (navMeshAgent == null)
                navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null)
                config = agent.GetComponent<MonsterConfig>();

            // Initialize data for this action run
            data.lookPoints = new Queue<Vector3>();
            data.stuckTimer = 0f;
            data.lastPosition = agent.Transform.position;

            // Generate random number of points based on config
            int pointsToGenerate = Random.Range(config.minInvestigatePoints, config.maxInvestigatePoints + 1);

            for (int i = 0; i < pointsToGenerate; i++)
            {
                Vector3 randomPoint = agent.Transform.position + Random.insideUnitSphere * config.investigateRadius;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, config.investigateRadius, NavMesh.AllAreas))
                {
                    data.lookPoints.Enqueue(hit.position);
                }
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // --- NEW: Unstuck Logic ---
            float distanceMoved = Vector3.Distance(agent.Transform.position, data.lastPosition);
            if (distanceMoved < config.stuckVelocityThreshold)
            {
                data.stuckTimer += context.DeltaTime;
            }
            else
            {
                data.stuckTimer = 0f;
                data.lastPosition = agent.Transform.position;
            }

            if (data.stuckTimer > config.maxStuckTime)
            {
                Debug.LogWarning("Agent is stuck while looking around. Stopping action.");
                return ActionRunState.Stop; // Action failed
            }
            
            // --- Completion Logic ---
            if (data.lookPoints.Count == 0 && (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance))
            {
                return ActionRunState.Completed;
            }

            // --- Movement Logic ---
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (data.lookPoints.Count > 0)
                {
                    // FIXED: We directly control the NavMeshAgent here.
                    // This is the correct way to handle multi-step movement within a single action.
                    navMeshAgent.SetDestination(data.lookPoints.Dequeue());
                }
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data) { }

        // UPDATED: Added unstuck variables
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public Queue<Vector3> lookPoints;
            public Vector3 lastPosition;
            public float stuckTimer;
        }
    }
}