using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class KidnapAction : GoapActionBase<KidnapAction.Data>
    {
        private KidnapMonsterConfig config;
        private MonsterMovement movement;
        private Transform playerTransform;

        // NEW: Inject Data Access via the Agent's Config
        // We will add the Energy SO reference to KidnapMonsterConfig later
        
        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            movement = agent.GetComponent<MonsterMovement>();
            
            if (data.Target is TransformTarget tt)
            {
                playerTransform = tt.Transform;
                movement.Chase(playerTransform, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (playerTransform == null) return ActionRunState.Stop;

            if (Vector3.Distance(agent.Transform.position, playerTransform.position) < 2.0f)
            {
                KidnapPlayer();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        private void KidnapPlayer()
        {
            if (config != null && config.currentEnergy != null && config.maxEnergy != null)
            {
                float drainAmount = config.maxEnergy.Value * config.energyDrainPercent;
                config.currentEnergy.ApplyChange(-drainAmount, 0f, config.maxEnergy.Value);
            }

            Vector3 randomDir = Random.insideUnitCircle.normalized;
            Vector3 teleportPos = playerTransform.position + new Vector3(randomDir.x, 0, randomDir.y) * config.teleportDistance;

            var controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            playerTransform.position = teleportPos;
            if (controller != null) controller.enabled = true;

            Debug.Log("Player Kidnapped!");
            var brain = config.GetComponent<MonsterBrain>(); 

            if (brain != null) 
            {
                brain.WipeMemory();
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}