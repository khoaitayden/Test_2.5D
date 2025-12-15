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

        // Use Brain's memory as the floor
        float timeFloor = brain.HandledNoiseTimestamp;

        foreach (var trace in traces)
        {
                if (trace.IsExpired) continue;
                if (trace.Timestamp <= timeFloor) continue;

                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                            trace.Type == TraceType.EnviromentNoiseStrong ||
                            trace.Type == TraceType.EnviromentNoiseMedium;

                if (!isLoud) continue;

                // Check distance
                if (Vector3.Distance(agentPos, trace.Position) > config.hearingRange) continue;

                if (trace.Timestamp > bestTime)
                {
                    bestTime = trace.Timestamp;
                    bestTrace = trace;
                }
            }

            if (bestTrace != null)
            {
                // THE FIX: Snap to NavMesh immediately.
                // If the sound was in the air, this brings the target down to the floor.
                if (UnityEngine.AI.NavMesh.SamplePosition(bestTrace.Position, out UnityEngine.AI.NavMeshHit hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    return new PositionTarget(hit.position);
                }
                
                // Fallback if sample fails (rare, but keeps behavior consistent)
                return new PositionTarget(bestTrace.Position);
            }

            return null;
            }
    }
}