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
        private MonsterBrain brain; // Added

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache components
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            touchSensor = agent.GetComponent<MonsterTouchSensor>();
            brain = agent.GetComponent<MonsterBrain>(); // Fetch Brain here

            data.startTime = Time.time;

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
                movement.Chase(targetTransform, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // 1. Success
            if (touchSensor.IsTouchingPlayer)
            {
                return ActionRunState.Completed;
            }

            // 2. Timeout / Failure
            if (Time.time > data.startTime + config.maxChaseTime)
            {
                // Call the new Brain method to search here
                if (brain != null) brain.OnMovementStuck();
                
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