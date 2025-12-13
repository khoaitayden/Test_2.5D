using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using System.Linq;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateNoiseAction : GoapActionBase<InvestigateNoiseAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        private Vector3 currentTargetPos;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();

            if (data.Target != null)
            {
                UpdateTarget(data.Target.Position);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // Dynamic Retargeting: If Sensor picks a newer trace, the Position changes
            if (Vector3.Distance(data.Target.Position, currentTargetPos) > 1.0f)
            {
                UpdateTarget(data.Target.Position);
            }

            // Arrival Check
            if (movement.HasArrivedOrStuck())
            {
                // We arrived! 
                // Mark the trace as Handled so we don't visit it again.
                MarkBestTraceAsHandled();
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

        private void MarkBestTraceAsHandled()
        {
            if (TraceManager.Instance == null) return;

            var traces = TraceManager.Instance.GetTraces();
            float bestTime = -1f;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;

                // FIX: Update filter to include your new noise types
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                            trace.Type == TraceType.EnviromentNoiseStrong ||
                            trace.Type == TraceType.EnviromentNoiseMedium; // <--- ADDED THIS

                if (!isLoud) continue;
                
                // Match trace to our location
                if (Vector3.Distance(trace.Position, currentTargetPos) < 2.0f)
                {
                    if (trace.Timestamp > bestTime) bestTime = trace.Timestamp;
                }
            }

            if (bestTime > 0)
            {
                // Debug.Log($"[NoiseAction] Marking noise at {bestTime} as HANDLED.");
                brain.MarkNoiseAsHandled(bestTime);
            }
            else
            {
                // Fallback: If we can't find the trace (maybe expired?), just mark 'Now' to break the loop
                // This prevents the infinite loop if the trace disappeared while moving.
                brain.MarkNoiseAsHandled(Time.time);
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}