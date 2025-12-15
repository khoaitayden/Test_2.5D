using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class FleeAction : GoapActionBase<FleeAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain; // Need brain reference
        
        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>(); // Get brain

            // Flee FROM the last known valid player position, not the live one (which might be null)
            Vector3 fleeFromPos = brain.LastKnownPlayerPosition != Vector3.zero 
                ? brain.LastKnownPlayerPosition 
                : (data.Target != null ? data.Target.Position : agent.Transform.position);

            Vector3 awayDir = (agent.Transform.position - fleeFromPos).normalized;
            // Ensure we don't just run backwards into a wall
            awayDir = Quaternion.Euler(0, Random.Range(-30, 30), 0) * awayDir; 
            
            Vector3 fleePos = agent.Transform.position + awayDir * 20.0f; 

            // Move fast
            movement.MoveTo(fleePos, config.chaseSpeed, config.stoppingDistance);
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Complete when we arrive or get stuck
            if (movement.HasArrivedOrStuck())
            {
                return ActionRunState.Completed;
            }
            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            // CRITICAL: Tell the brain we are done fleeing!
            brain?.OnFleeComplete();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; } 
        }
    }
}