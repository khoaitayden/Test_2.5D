using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class HideAction : GoapActionBase<HideAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfigBase config;
        private KidnapMonsterBrain brain; // Add reference

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfigBase>();
            brain = agent.GetComponent<KidnapMonsterBrain>(); 

            if (data.Target != null)
            {
                movement.MoveTo(data.Target.Position, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (movement.HasArrivedOrStuck())
            {
                return ActionRunState.Completed;
            }
            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            if (brain != null) brain.OnHideComplete();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}