using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class LoudTraceSensor : LocalTargetSensorBase
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
            
            GameTrace bestTrace = null;
            float bestTime = -1f;

            float timeFloor = brain.HandledNoiseTimestamp;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                if (trace.Timestamp <= timeFloor) continue;

                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium;

                if (!isLoud) continue;
                if (Vector3.Distance(agentPos, trace.Position) > config.hearingRange) continue;

                if (trace.Timestamp > bestTime)
                {
                    bestTime = trace.Timestamp;
                    bestTrace = trace;
                }
            }

            if (bestTrace != null)
            {
                // 1. Precision Check (Configurable)
                if (NavMesh.SamplePosition(bestTrace.Position, out NavMeshHit hitPrecision, config.traceNavMeshSnapRadius, NavMesh.AllAreas))
                {
                    return new PositionTarget(hitPrecision.position);
                }

                // 2. Fallback Check (Configurable)
                if (NavMesh.SamplePosition(bestTrace.Position, out NavMeshHit hitWide, config.traceNavMeshFallbackRadius, NavMesh.AllAreas))
                {
                    return new PositionTarget(hitWide.position);
                }
                
                return new PositionTarget(bestTrace.Position);
            }

            return null;
        }
    }
}