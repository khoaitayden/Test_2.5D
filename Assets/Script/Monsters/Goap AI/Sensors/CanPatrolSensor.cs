using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class CanPatrolSensor : LocalWorldSensorBase
    {
        // Cache reference to the Hearing Sensor to reuse logic
        private IsHearingNoiseSensor hearingSensor;

        public override void Created() 
        {
            // We can't easily reference another sensor instance directly in this framework usually,
            // so we will just re-check the Brain/Config state.
        }
        
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            var brain = references.GetCachedComponent<MonsterBrain>();
            
            // We need to check if we hear noise.
            // Since we can't easily ask the other sensor, we must check the condition manually
            // OR check if the planner has set the World State (which is circular).
            
            // ROBUST WAY: Use the Brain as the source of truth? No, hearing is instant.
            // Let's perform the cheap check here.
            
            bool isHearingNoise = CheckHearing(agent, references);

            if (brain == null) return 0;

            // Logic: Can only patrol if:
            // 1. Not Seeing Player
            // 2. Not Investigating (Lost Player)
            // 3. Not Hearing Noise (New!)
            bool busy = brain.IsPlayerVisible || brain.IsInvestigating || isHearingNoise;

            return busy ? 0 : 1;
        }

        private bool CheckHearing(IActionReceiver agent, IComponentReference references)
        {
            if (TraceManager.Instance == null) return false;
            var config = references.GetCachedComponent<MonsterConfig>();
            if (config == null) return false;

            var traces = TraceManager.Instance.GetTraces();
            Vector3 pos = agent.Transform.position;

            // Fast check: just loop and find one valid sound
            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                
                // Filter loud types
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium ||
                              trace.Type == TraceType.Footstep_Jump;
                
                if(!isLoud) continue;

                float d = Vector3.Distance(pos, trace.Position);
                // FIX: Match the IsHearingNoiseSensor threshold (1.0f)
                if (d <= config.hearingRange && d > 1.0f) return true;
            }
            return false;
        }
    }
}