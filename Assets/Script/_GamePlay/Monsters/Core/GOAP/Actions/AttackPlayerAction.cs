using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfigBase config;
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            InitializeDependencies(agent);
            
            brain.IsAttacking = true;
            data.startTime = Time.time;

            Transform targetTransform = ResolveTarget(data);

            if (targetTransform != null)
            {
                movement.Chase(targetTransform, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (Time.time > data.startTime + config.maxChaseTime)
            {
                brain?.OnMovementStuck();
                return ActionRunState.Stop;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            if (brain != null) brain.IsAttacking = false;
        }


        private void InitializeDependencies(IMonoAgent agent)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfigBase>();
            brain = agent.GetComponent<MonsterBrain>();
        }

        private Transform ResolveTarget(Data data)
        {
            if (data.Target is TransformTarget tt && tt.Transform != null)
                return tt.Transform;
            if (brain != null && brain.PlayerAnchor != null && brain.PlayerAnchor.Value != null)
            {
                return brain.PlayerAnchor.Value;
            }

            if (brain != null && brain.CurrentPlayerTarget != null)
                return brain.CurrentPlayerTarget;

            return null;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}