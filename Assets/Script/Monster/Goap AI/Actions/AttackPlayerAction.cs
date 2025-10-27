// In AttackPlayerAction.cs
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
        private MonsterConfig config;

        // Path update timer to prevent spamming SetDestination
        private float pathUpdateTimer;
        private readonly float pathUpdateDelay = 0.1f;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            Debug.Log("Start attack");
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (touchSensor == null) touchSensor = agent.GetComponent<MonsterTouchSensor>();

            // SET AGGRESSIVE CHASE SPEED
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.Chase);
            pathUpdateTimer = 0f;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            Debug.Log("Attack time");
            if (data.Target == null)
            {
                Debug.LogWarning("[AttackPlayerAction] Target lost, stopping action.");
                return ActionRunState.Stop;
            }
            
            // Check if we killed the player by touch
            if (touchSensor != null && touchSensor.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                // Here you would add game logic to actually kill/despawn the player
                return ActionRunState.Completed;
            }

            // Performance optimization: only update the path every few frames
            pathUpdateTimer -= context.DeltaTime;
            if (pathUpdateTimer <= 0f)
            {
                navMeshAgent.SetDestination(data.Target.Position);
                pathUpdateTimer = pathUpdateDelay;
            }
            
            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data)
        {
            // The next action's Start() will set the speed, so no need to do anything here.
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}