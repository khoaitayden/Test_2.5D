using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class FreshTraceSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;
        private MonsterBrain brain;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
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
                if (trace.Timestamp <= handledTime) continue; // Already handled

                bool isValid = false;

                // A. HEARING CHECK (Loud)
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium;
                
                if (isLoud && Vector3.Distance(agentPos, trace.Position) <= config.hearingRange)
                {
                    isValid = true;
                }

                // B. VISION CHECK (Footsteps)
                bool isFootstep = trace.Type == TraceType.Footstep_Walk || 
                                  trace.Type == TraceType.Footstep_Run;

                if (isFootstep && Vector3.Distance(agentPos, trace.Position) <= config.viewRadius)
                {
                    Vector3 dirToTrace = (trace.Position - agentPos).normalized;
                    // Check if inside Vision Cone angle
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
                    return new PositionTarget(hit.position);
                }
                return new PositionTarget(bestTrace.Position);
            }

            return null;
        }
    }
}