using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsHearingNoiseSensor : LocalWorldSensorBase
    {
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            
            if (TraceManager.Instance == null) return 0;

            var traces = TraceManager.Instance.GetTraces();
            Vector3 agentPos = agent.Transform.position;

            // Iterate backwards to find newest relevant trace
            for (int i = traces.Count - 1; i >= 0; i--)
            {
                var trace = traces[i];

                // 1. Check if Expired (Cleanest way)
                if (trace.IsExpired) continue;

                // 2. Check Type
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium ||
                              trace.Type == TraceType.Footstep_Jump;

                if (!isLoud) continue;

                // 3. Check Distance
                float dist = Vector3.Distance(agentPos, trace.Position);
                
                // Debug Logic: Uncomment to see what the monster hears
                // Debug.Log($"[HearingSensor] Found {trace.Type} at Dist {dist:F1}. Range: {config.hearingRange}");

                if (dist > config.hearingRange) continue;

                // 4. Ignore sounds we are standing on (to prevent loops)
                if (dist < 0.5f) continue;

                // Found a valid sound!
                return 1;
            }

            return 0;
        }
    }
}