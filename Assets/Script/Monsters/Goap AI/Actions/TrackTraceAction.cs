using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class TrackTraceAction : GoapActionBase<TrackTraceAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        
        // We removed the sensor variable here to fix the error.
        // We will do the check directly in this script.

        private Vector3 currentDestination;
        private float currentTimestamp; 

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();

            if (data.Target != null)
            {
                UpdateDestination(data.Target.Position);
                // Assume the target provided is slightly newer than what we handled
                currentTimestamp = brain.HandledNoiseTimestamp + 0.01f; 
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- 1. ACTIVE POLLING ---
            // Check if a NEWER footprint/noise appeared while we were walking
            Vector3? betterTargetPos = CheckForBetterTarget(agent);
            
            if (betterTargetPos.HasValue)
            {
                // If the new target is significantly different from where we are going...
                if (Vector3.Distance(betterTargetPos.Value, currentDestination) > 1.0f)
                {
                    UpdateDestination(betterTargetPos.Value);
                }
            }

            // --- 2. ARRIVAL ---
            if (movement.HasArrivedOrStuck())
            {
                // We reached the source. Mark as handled.
                brain.MarkNoiseAsHandled(Time.time);
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

        // --- INTERNAL SCAN LOGIC ---
        // This copies the logic from FreshTraceSensor so we don't need to rely on external references.
        private Vector3? CheckForBetterTarget(IMonoAgent agent)
        {
            if (TraceManager.Instance == null) return null;

            var traces = TraceManager.Instance.GetTraces();
            Vector3 agentPos = agent.Transform.position;
            Vector3 agentForward = agent.Transform.forward;
            
            GameTrace bestTrace = null;
            float bestTime = -1f;
            float handledTime = brain.HandledNoiseTimestamp;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                if (trace.Timestamp <= handledTime) continue; // Must be unhandled

                bool isValid = false;

                // A. HEARING (Loud)
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium;
                
                if (isLoud && Vector3.Distance(agentPos, trace.Position) <= config.hearingRange)
                {
                    isValid = true;
                }

                // B. VISION (Footsteps)
                bool isFootstep = trace.Type == TraceType.Footstep_Walk || 
                                  trace.Type == TraceType.Footstep_Run;

                if (isFootstep && Vector3.Distance(agentPos, trace.Position) <= config.viewRadius)
                {
                    Vector3 dirToTrace = (trace.Position - agentPos).normalized;
                    // Check Angle
                    if (Vector3.Angle(agentForward, dirToTrace) < config.ViewAngle / 2f)
                    {
                        isValid = true; 
                    }
                }

                if (!isValid) continue;

                // Pick NEWEST
                if (trace.Timestamp > bestTime)
                {
                    bestTime = trace.Timestamp;
                    bestTrace = trace;
                }
            }

            if (bestTrace != null)
            {
                // Snap to NavMesh
                if (NavMesh.SamplePosition(bestTrace.Position, out NavMeshHit hit, config.traceNavMeshSnapRadius, NavMesh.AllAreas))
                {
                    return hit.position;
                }
                return bestTrace.Position;
            }

            return null;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}