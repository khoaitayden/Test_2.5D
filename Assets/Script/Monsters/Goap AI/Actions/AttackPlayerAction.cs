using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterTouchSensor touchSensor;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (touchSensor == null) touchSensor = agent.GetComponent<MonsterTouchSensor>();

            data.startTime = Time.time;

            // --- CHASE LOGIC START ---
            Transform targetTransform = null;

            if (data.Target is TransformTarget tt && tt.Transform != null)
            {
                targetTransform = tt.Transform;
            }
            else
            {
                // Fallback search
                var player = GameObject.FindWithTag("Player");
                if (player != null) targetTransform = player.transform;
            }

            if (targetTransform != null)
            {
                Debug.Log($"[AttackAction] Starting Chase: {targetTransform.name}");
                movement.Chase(targetTransform);
            }
            else
            {
                Debug.LogWarning("[AttackAction] No target to chase!");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Only check completion conditions here. Movement is handled by MonsterMovement.Update.

            // 1. Success
            if (touchSensor.IsTouchingPlayer)
            {
                return ActionRunState.Completed;
            }

            // 2. Timeout
            if (Time.time > data.startTime + config.maxChaseTime)
            {
                Debug.Log("[AttackAction] Timed out.");
                return ActionRunState.Stop;
            }

            // 3. Keep Running
            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}