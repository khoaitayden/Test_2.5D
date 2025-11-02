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
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            Debug.Log("Start attack: Setting speed and handing over movement to MonsterMoveBehaviour.");
            
            // This setup is still required. The action is responsible for setting the correct speed mode.
            if (navMeshAgent == null) navMeshAgent = agent.GetComponent<NavMeshAgent>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (touchSensor == null) touchSensor = agent.GetComponent<MonsterTouchSensor>();
            brain ??= agent.GetComponent<MonsterBrain>();
            // SET AGGRESSIVE CHASE SPEED
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.Chase);
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            Debug.Log("Attack time: Checking for touch. Movement is handled by another script.");
            
            // Safety check: if the GOAP planner loses the target, we should stop.
            if (data.Target == null)
            {
                Debug.LogWarning("[AttackPlayerAction] Target lost, stopping action.");
                return ActionRunState.Stop;
            }

            // This action's ONLY job is now to check for the success condition.
            // MonsterMoveBehaviour is handling the navMeshAgent.SetDestination calls.
            if (touchSensor != null && touchSensor.IsTouchingPlayer)
            {
                Debug.Log("PLAYER KILLED BY TOUCH!");
                return ActionRunState.Completed;
            }
            
            // We return Continue to signal that this action is still active.
            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data)
        {
            Debug.Log("Attack action ended.");
            // Report to the brain that the attack is over.
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}