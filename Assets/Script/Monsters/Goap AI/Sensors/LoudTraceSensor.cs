using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using System.Collections.Generic;

namespace CrashKonijn.Goap.MonsterGen
{
    public class LoudTraceSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;
        
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            if (TraceManager.Instance == null) return null;

            var traces = TraceManager.Instance.GetTraces();
            Vector3 agentPos = agent.Transform.position;
            
            GameTrace bestTrace = null;
            float newestTime = -1f;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;

                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium ||
                              trace.Type == TraceType.Footstep_Jump;

                if (!isLoud) continue;

                float dist = Vector3.Distance(agentPos, trace.Position);
                if (dist > config.hearingRange) continue;
                if (dist < 2.5f) continue;

                // Pick Newest
                if (trace.Timestamp > newestTime)
                {
                    newestTime = trace.Timestamp;
                    bestTrace = trace;
                }
            }

            if (bestTrace != null)
            {
                // Debug.Log($"[LoudTraceSensor] Targeting {bestTrace.Type} at {bestTrace.Position}");
                return new PositionTarget(bestTrace.Position);
            }

            return null;
        }
    }
}