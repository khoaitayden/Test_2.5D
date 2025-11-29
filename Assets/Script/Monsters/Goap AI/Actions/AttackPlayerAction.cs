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
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            touchSensor = agent.GetComponent<MonsterTouchSensor>();

            data.startTime = Time.time;

            // --- CHASE LOGIC START ---
            Transform targetTransform = null;

            if (data.Target is TransformTarget tt && tt.Transform != null)
            {
                targetTransform = tt.Transform;
            }
            else
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) targetTransform = player.transform;
            }

            if (targetTransform != null)
            {
                // FIX: Pass the chase speed explicitly
                movement.Chase(targetTransform, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (touchSensor.IsTouchingPlayer)
            {
                return ActionRunState.Completed;
            }

            if (Time.time > data.startTime + config.maxChaseTime)
            {
                return ActionRunState.Stop;
            }

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