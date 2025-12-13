using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using System.Collections.Generic;

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

            // Use Brain's memory as the floor. Ignore anything older.
            float timeFloor = brain.HandledNoiseTimestamp;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;

                // 1. Logic Check (Newer than what we've done)
                // This removes the "Revisit" issue without magic numbers
                if (trace.Timestamp <= timeFloor) continue;

                // 2. Type Check
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium;

                if (!isLoud) continue;

                // 3. Range Check
                if (Vector3.Distance(agentPos, trace.Position) > config.hearingRange) continue;

                // 4. Find the absolute newest
                if (trace.Timestamp > bestTime)
                {
                    bestTime = trace.Timestamp;
                    bestTrace = trace;
                }
            }

            if (bestTrace != null)
            {
                // We return the Position, but we verify it inside the Action later using Time
                return new PositionTarget(bestTrace.Position);
            }

            return null;
        }
    }
}