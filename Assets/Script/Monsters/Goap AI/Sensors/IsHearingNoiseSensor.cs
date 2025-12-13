using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsHearingNoiseSensor : LocalWorldSensorBase
    {
        private MonsterConfig config;
        private MonsterBrain brain;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (TraceManager.Instance == null) return 0;

            var traces = TraceManager.Instance.GetTraces();
            Vector3 agentPos = agent.Transform.position;
            float timeFloor = brain.HandledNoiseTimestamp;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                if (trace.Timestamp <= timeFloor) continue; // Ignore handled/old noises

                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium;

                if (isLoud && Vector3.Distance(agentPos, trace.Position) <= config.hearingRange)
                {
                    return 1; // There is a NEW, UNHANDLED noise in range
                }
            }

            return 0;
        }
    }
}