using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using UnityEngine.AI; // Needed for NavMesh sampling

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateNoiseAction : GoapActionBase<InvestigateNoiseAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        
        private Vector3 currentDestination;
        private float currentTraceTimestamp; // Track the time of the noise we are chasing

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();

            if (data.Target != null)
            {
                // 1. Initial Setup
                UpdateDestination(data.Target.Position);
                
                // 2. Try to guess the timestamp of the target we were given so we can compare later
                // (We default to the Brain's floor if we can't match it, just to be safe)
                currentTraceTimestamp = FindTimestampForPosition(data.Target.Position) ?? brain.HandledNoiseTimestamp;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- 1. ACTIVE SCAN FOR NEWER NOISE ---
            // We duplicate the sensor logic here briefly to find if there is a "Better Offer"
            GameTrace betterTrace = CheckForNewerTrace(agent.Transform.position);

            if (betterTrace != null)
            {
                // We found a noise NEWER than the one we are currently walking to
                // Debug.Log($"[Investigate] Switched to newer noise! ({currentTraceTimestamp} -> {betterTrace.Timestamp})");
                
                currentTraceTimestamp = betterTrace.Timestamp;
                
                // Snap to NavMesh (matching Sensor logic) to ensure valid path
                Vector3 targetPos = betterTrace.Position;
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, config.traceNavMeshSnapRadius, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }

                UpdateDestination(targetPos);
            }

            // --- 2. ARRIVAL CHECK ---
            if (movement.HasArrivedOrStuck())
            {
                // We arrived. Mark this timestamp as handled.
                brain.MarkNoiseAsHandled(currentTraceTimestamp);
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        private void UpdateDestination(Vector3 pos)
        {
            currentDestination = pos;
            movement.MoveTo(pos, config.investigateSpeed, config.stoppingDistance);
        }

        // Helper to find the "Best" trace currently available
        private GameTrace CheckForNewerTrace(Vector3 agentPos)
        {
            if (TraceManager.Instance == null) return null;

            var traces = TraceManager.Instance.GetTraces();
            GameTrace bestCandidate = null;
            float bestTime = currentTraceTimestamp; // We only care if it's newer than CURRENT

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                if (trace.Timestamp <= bestTime) continue; // Must be newer than what we have

                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium;

                if (!isLoud) continue;
                if (Vector3.Distance(agentPos, trace.Position) > config.hearingRange) continue;

                bestTime = trace.Timestamp;
                bestCandidate = trace;
            }

            return bestCandidate;
        }

        // Helper to match a position back to a timestamp (for Start)
        private float? FindTimestampForPosition(Vector3 pos)
        {
            if (TraceManager.Instance == null) return null;
            var traces = TraceManager.Instance.GetTraces();
            
            foreach (var trace in traces)
            {
                if (Vector3.Distance(trace.Position, pos) < 1.0f) // Loose match
                    return trace.Timestamp;
            }
            return null;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}