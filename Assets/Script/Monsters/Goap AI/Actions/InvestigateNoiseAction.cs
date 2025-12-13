using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateNoiseAction : GoapActionBase<InvestigateNoiseAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();

            if (data.Target != null)
            {
                // Go to the noise!
                movement.MoveTo(data.Target.Position, config.investigateSpeed, config.stoppingDistance);
                Debug.Log($"[NoiseAction] Heard something at {data.Target.Position}. Investigating.");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // FIX: If we are close enough that the sensor MIGHT flick off, force complete.
            // Distance check here acts as a "Success" trigger.
            // Using 2.0f here ensures we finish BEFORE the sensor (at 1.0f) turns off the lights.
            if (Vector3.Distance(agent.Transform.position, data.Target.Position) <= 1.5f)
            {
                return ActionRunState.Completed;
            }

            if (movement.HasArrivedOrStuck())
            {
                return ActionRunState.Completed;
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
        }
    }
}