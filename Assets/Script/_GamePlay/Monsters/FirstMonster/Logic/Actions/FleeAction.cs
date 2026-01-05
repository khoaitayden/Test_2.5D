using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class FleeAction : GoapActionBase<FleeAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        
        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>(); 

            // Flee FROM the last known valid player position
            Vector3 fleeFromPos = brain.LastKnownPlayerPosition != Vector3.zero 
                ? brain.LastKnownPlayerPosition 
                : (data.Target != null ? data.Target.Position : agent.Transform.position);

            Vector3 awayDir = (agent.Transform.position - fleeFromPos).normalized;
            awayDir = Quaternion.Euler(0, Random.Range(-30, 30), 0) * awayDir; 
            
            // --- UPDATED: Uses Config Variable ---
            Vector3 fleePos = agent.Transform.position + awayDir * config.fleeRunDistance; 

            // Move fast
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
            brain?.OnFleeComplete();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; } 
        }
    }
}