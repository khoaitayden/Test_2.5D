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
        
        // Need to check hearing directly to interrupt
        // Ideally we check the Sensor, but accessing sensors inside Action is hard.
        // We will do a lightweight check via TraceManager.

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();

            if (data.Target != null)
            {
                movement.MoveTo(data.Target.Position, config.patrolSpeed, config.stoppingDistance);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- INTERRUPT LOGIC ---
            // If there is a valid noise, stop patrolling immediately.
            // The Planner will then see "InvestigateNoise" is available and pick it.
            if (CheckForNoise(agent.Transform.position))
            {
                // Debug.Log("[PatrolAction] Interrupted by Noise!");
                return ActionRunState.Completed; 
            }
            // -----------------------

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

        // Lightweight check similar to the sensor
        private bool CheckForNoise(Vector3 pos)
        {
            if (TraceManager.Instance == null) return false;
            var brain = movement.GetComponent<MonsterBrain>(); // Hacky but works
            if (brain == null) return false;

            var traces = TraceManager.Instance.GetTraces();
            float timeFloor = brain.HandledNoiseTimestamp;

            // Iterate backwards for speed
            for (int i = traces.Count - 1; i >= 0; i--)
            {
                var t = traces[i];
                if (t.IsExpired || t.Timestamp <= timeFloor) continue;

                // Only interrupt for LOUD noises
                bool isLoud = t.Type == TraceType.Soul_Collection || 
                              t.Type == TraceType.EnviromentNoiseStrong ||
                              t.Type == TraceType.EnviromentNoiseMedium; // Including Jump

                if (isLoud && Vector3.Distance(pos, t.Position) <= config.hearingRange)
                {
                    // Found a valid, unhandled noise
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