// FILE TO REPLACE: AttackPlayerAction.cs (Fully Self-Contained)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        private MonsterTouchSensor touchSensor;
        private NavMeshAgent navMeshAgent;
        
        private float pathUpdateTimer;
        private const float PathUpdateDelay = 0.2f;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache components
            if (touchSensor == null) touchSensor = agent.GetComponent<MonsterTouchSensor>();
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            
            // THE FIX: Take full control of the NavMeshAgent
            if (data.Target != null)
            {
                // Immediately start moving towards the player's current position.
                navMeshAgent.SetDestination(data.Target.Position);
                navMeshAgent.isStopped = false; // Explicitly tell the agent to GO!
            }
            
            // Initialize the timer for subsequent updates.
            pathUpdateTimer = PathUpdateDelay;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null || touchSensor == null) return ActionRunState.Stop;

            // Completion Condition: If we touch the player, the action is done.
            if (touchSensor.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                return ActionRunState.Completed;
            }
            
            // Continuous Chasing: Periodically update the destination to the player's new position.
            pathUpdateTimer -= context.DeltaTime;
            if (pathUpdateTimer <= 0f)
            {
                // This is a TransformTarget, so data.Target.Position is always up-to-date.
                navMeshAgent.SetDestination(data.Target.Position);
                pathUpdateTimer = PathUpdateDelay;
            }

            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data)
        {
            // Robust Cleanup: When the action ends, stop all movement.
             if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
             {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
             }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}