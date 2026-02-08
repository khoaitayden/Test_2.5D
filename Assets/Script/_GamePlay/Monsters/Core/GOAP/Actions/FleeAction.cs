using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class FleeAction : GoapActionBase<FleeAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfigBase config;
        private MonsterBrain brain;
        
        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfigBase>();
            brain = agent.GetComponent<MonsterBrain>(); 
            Debug.Log("Try to flee");
            Vector3 fleeFromPos = brain.LastKnownPlayerPosition != Vector3.zero 
                ? brain.LastKnownPlayerPosition 
                : (data.Target != null ? data.Target.Position : agent.Transform.position);

            Vector3 awayDir = (agent.Transform.position - fleeFromPos).normalized;
            awayDir = Quaternion.Euler(0, Random.Range(-30, 30), 0) * awayDir; 
            
            Vector3 fleePos = agent.Transform.position + awayDir * config.fleeRunDistance; 

            movement.MoveTo(fleePos, config.chaseSpeed);
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
            var brain = agent.GetComponent<KidnapMonsterBrain>();
            if(brain != null) {
                brain.OnSafetyAchieved();
                //brain.IsSafe=true;
            };
            
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; } 
        }
    }
}