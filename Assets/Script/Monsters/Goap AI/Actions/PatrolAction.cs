using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain; // Added reference

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>(); // Fetch it here

            if (data.Target != null)
            {
                movement.MoveTo(data.Target.Position, config.patrolSpeed, config.stoppingDistance);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- INTERRUPT LOGIC (Noise) ---
            if (CheckForNoise(agent.Transform.position))
            {
                return ActionRunState.Completed; 
            }

            // --- STUCK / ARRIVAL LOGIC ---
            if (movement.HasArrivedOrStuck())
            {
                // If we stopped but are far away (> 5m), we are stuck. Flee!
                if (Vector3.Distance(agent.Transform.position, data.Target.Position) > 5.0f)
                {
                    Debug.LogWarning("[Patrol] Stuck far from target. Fleeing.");
                    if (brain != null) brain.OnMovementStuck();
                    return ActionRunState.Stop;
                }

                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        private bool CheckForNoise(Vector3 pos)
        {
            if (TraceManager.Instance == null || brain == null) return false;

            var traces = TraceManager.Instance.GetTraces();
            float timeFloor = brain.HandledNoiseTimestamp;

            for (int i = traces.Count - 1; i >= 0; i--)
            {
                var t = traces[i];
                if (t.IsExpired || t.Timestamp <= timeFloor) continue;

                bool isLoud = t.Type == TraceType.Soul_Collection || 
                              t.Type == TraceType.EnviromentNoiseStrong ||
                              t.Type == TraceType.EnviromentNoiseMedium;

                if (isLoud && Vector3.Distance(pos, t.Position) <= config.hearingRange)
                {
                    return true;
                }
            }
            return false;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}