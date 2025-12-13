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
        
        // Track current target to detect changes
        private Vector3 currentTargetPos;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();

            if (data.Target != null)
            {
                UpdateTarget(data.Target.Position);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- DYNAMIC RETARGETING FIX ---
            // Check if the sensor found a NEW, better noise (Target position changed)
            if (Vector3.Distance(data.Target.Position, currentTargetPos) > 1.0f)
            {
                Debug.Log($"[NoiseAction] Newer noise detected! Switching target to {data.Target.Position}");
                UpdateTarget(data.Target.Position);
            }
            // -------------------------------

            // Completion Logic
            if (Vector3.Distance(agent.Transform.position, currentTargetPos) <= 2.0f)
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

        private void UpdateTarget(Vector3 pos)
        {
            currentTargetPos = pos;
            movement.MoveTo(pos, config.investigateSpeed, config.stoppingDistance);
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}