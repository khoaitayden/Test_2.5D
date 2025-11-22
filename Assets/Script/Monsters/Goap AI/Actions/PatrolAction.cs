using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.MonsterGen.Capabilities; // Note new namespace
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        private MonsterMovement movement;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache the movement component once
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            
            if (data.Target != null)
            {
                // ONE LINE COMMAND: "Go to this target, gently."
                movement.GoTo(data.Target.Position, MonsterMovement.SpeedState.Patrol);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // SIMPLE CHECKS
            if (movement.IsStuck)
            {
                // Optional: You could ask Brain to blacklist this location here
                return ActionRunState.Stop; 
            }

            if (movement.HasArrived)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            // Good practice to stop movement when switching tasks, 
            // unless you want fluidity between move actions.
            // For now, let's stop for safety.
            movement.Stop();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}