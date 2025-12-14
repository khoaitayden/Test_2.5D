using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateNoiseAction : GoapActionBase<InvestigateNoiseAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        
        // Track where we are currently going
        private Vector3 currentDestination;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();

            if (data.Target != null)
            {
                UpdateDestination(data.Target.Position);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- INTERRUPT / SWITCH LOGIC ---
            // Check if the sensor found a NEW noise (Target changed significantly)
            if (Vector3.Distance(data.Target.Position, currentDestination) > 2.0f)
            {
                // Debug.Log($"[NoiseAction] Switching to newer noise at {data.Target.Position}");
                UpdateDestination(data.Target.Position);
            }
            // --------------------------------

            // Check Arrival at CURRENT destination
            if (movement.HasArrivedOrStuck())
            {
                // We reached the noise we were aiming for.
                // Mark THIS specific noise (at currentDestination) as handled.
                MarkNoiseAtLocationAsHandled(currentDestination);
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

        private void MarkNoiseAtLocationAsHandled(Vector3 location)
        {
            if (TraceManager.Instance == null) return;

            var traces = TraceManager.Instance.GetTraces();
            float bestTime = -1f;

            foreach (var trace in traces)
            {
                if (trace.IsExpired) continue;
                
                // Only look for loud noises near where we arrived
                bool isLoud = trace.Type == TraceType.Soul_Collection || 
                              trace.Type == TraceType.EnviromentNoiseStrong ||
                              trace.Type == TraceType.EnviromentNoiseMedium; 

                if (!isLoud) continue;

                if (Vector3.Distance(trace.Position, location) < 2.5f)
                {
                    if (trace.Timestamp > bestTime) bestTime = trace.Timestamp;
                }
            }

            if (bestTime > 0)
            {
                brain.MarkNoiseAsHandled(bestTime);
            }
            else
            {
                // Safety: if trace is gone, mark current time so we don't loop
                brain.MarkNoiseAsHandled(Time.time);
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}