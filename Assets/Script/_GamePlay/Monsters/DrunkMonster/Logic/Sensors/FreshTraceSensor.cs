using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

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

            var traces = brain.TraceStorage.GetTraces();
            Vector3 eyes = agent.Transform.position;
            Vector3 facing = agent.Transform.forward;
            float timeFloor = brain.HandledNoiseTimestamp;
            
            GameTrace bestTrace = null;

            foreach (var t in traces)
            {
                if (t.IsExpired || t.Timestamp <= timeFloor) continue;

                bool isDetected = false;

                // 2. Hearing Check
                if (IsLoud(t.Type))
                {
                    if (Vector3.Distance(eyes, t.Position) <= config.hearingRange)
                        isDetected = true;
                }
                // 3. Vision Check
                else 
                {
                    // Check Distance first (Cheap)
                    if (Vector3.Distance(eyes, t.Position) <= config.viewRadius)
                    {
                        // Check Angle (Expensive)
                        Vector3 dir = (t.Position - eyes).normalized;
                        if (Vector3.Angle(facing, dir) < config.ViewAngle / 2f)
                            isDetected = true;
                    }
                }

                if (!isDetected) continue;

                // 4. Comparison: Newer is better
                if (bestTrace == null || t.Timestamp > bestTrace.Timestamp)
                {
                    bestTrace = t;
                }
            }

            if (bestTrace != null)
            {
                // Return Raw Position. MoveTo() will handle safety snapping.
                return new PositionTarget(bestTrace.Position);
            }

            return null;
        }

        private bool IsLoud(TraceType t)
        {
            return t == TraceType.Soul_Collection || 
                   t == TraceType.EnviromentNoiseStrong || 
                   t == TraceType.EnviromentNoiseMedium;
        }
    }
}