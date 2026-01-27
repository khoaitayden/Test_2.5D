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
        private MonsterBrain brain;
        private Transform playerTransform;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            movement = agent.GetComponent<MonsterMovement>();
            brain = agent.GetComponent<MonsterBrain>();
            
            data.startTime = Time.time; 

            if (data.Target is TransformTarget tt)
            {
                playerTransform = tt.Transform;
                movement.Chase(playerTransform, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (Time.time > data.startTime + config.maxChaseTime)
            {
                brain?.OnMovementStuck();
                return ActionRunState.Stop;
            }

            if (playerTransform == null) return ActionRunState.Stop;

            // 2. Success Check
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
            if (brain != null) brain.WipeMemory();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}