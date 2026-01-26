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
            // 1. Drain Energy directly via config reference
            if (config != null && config.currentEnergy != null && config.maxEnergy != null)
            {
                float drainAmount = config.maxEnergy.Value * config.energyDrainPercent;
                // Subtract energy (ensure we don't go below 0)
                config.currentEnergy.ApplyChange(-drainAmount, 0f, config.maxEnergy.Value);
            }

            // 2. Find Teleport Point
            // (Assuming you have a safer way to find navmesh points, but random circle works for prototype)
            Vector3 randomDir = Random.insideUnitCircle.normalized;
            Vector3 teleportPos = playerTransform.position + new Vector3(randomDir.x, 0, randomDir.y) * config.teleportDistance;

            // 3. Teleport
            var controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            playerTransform.position = teleportPos;
            if (controller != null) controller.enabled = true;

            Debug.Log("Player Kidnapped!");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}