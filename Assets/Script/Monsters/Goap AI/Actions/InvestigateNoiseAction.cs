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
            // Find the timestamp of the trace we just visited (or the current newest one)
            // Since we always target the "Best" trace, we can just grab the best timestamp from manager
            if (TraceManager.Instance == null) return;

            var traces = TraceManager.Instance.GetTraces();
            float bestTime = -1f;

            // Logic mirrors LoudTraceSensor
            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                bool isLoud = trace.Type == TraceType.Soul_Collection;
                if (!isLoud) continue;
                
                // We only care about traces that match where we just went
                // Using a loose distance check to match the trace to our target position
                if (Vector3.Distance(trace.Position, currentTargetPos) < 2.0f)
                {
                    if (trace.Timestamp > bestTime) bestTime = trace.Timestamp;
                }
            }

            if (bestTime > 0)
            {
                brain.MarkNoiseAsHandled(bestTime);
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}