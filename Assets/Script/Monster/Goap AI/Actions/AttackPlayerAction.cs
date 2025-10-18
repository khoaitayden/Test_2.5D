// In Assets/Monster/Goap AI/Actions/AttackPlayerAction.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        private MonsterConfig config;

        // This is the correct override for Created() in an Action
        public override void Created() { }
        
        public override void Start(IMonoAgent agent, Data data) { }
        public override void End(IMonoAgent agent, Data data) { }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Cache config on first run
            if (config == null)
                config = agent.GetComponent<MonsterConfig>();

            if (data.Target == null || config == null)
                return ActionRunState.Stop;

            var distance = Vector3.Distance(agent.Transform.position, data.Target.Position);

            if (distance < config.AttackDistance)
            {
                Debug.Log("PLAYER KILLED!");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}