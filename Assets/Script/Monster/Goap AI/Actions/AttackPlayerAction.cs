// FILE TO EDIT: AttackPlayerAction.cs (UPGRADED)
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
        
        // Timer to prevent updating the path every single frame
        private float pathUpdateTimer;
        private const float PathUpdateDelay = 0.2f;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (touchSensor == null) touchSensor = agent.GetComponent<MonsterTouchSensor>();
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null || touchSensor == null) return ActionRunState.Stop;

            if (touchSensor.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                return ActionRunState.Completed;
            }
            
            // Update the path periodically
            pathUpdateTimer -= context.DeltaTime;
            if (pathUpdateTimer <= 0f)
            {
                navMeshAgent.SetDestination(data.Target.Position);
                pathUpdateTimer = PathUpdateDelay;
            }

            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data)
        {
             if (navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}