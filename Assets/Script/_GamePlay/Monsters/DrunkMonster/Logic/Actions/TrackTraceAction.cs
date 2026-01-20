using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class TrackTraceAction : GoapActionBase<TrackTraceAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        
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

            if (movement.HasArrivedOrStuck())
            {
                brain.MarkNoiseAsHandled(Time.time+0.01f);
                return ActionRunState.Completed;
            }


            Vector3? newerPos = ScanForNewerTrace(agent);
            
            if (newerPos.HasValue)
            {

                if (Vector3.Distance(newerPos.Value, currentDestination) > 1.0f)
                {
                    UpdateDestination(newerPos.Value);
                }
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

            movement.MoveTo(pos, config.investigateSpeed);
        }


        private Vector3? ScanForNewerTrace(IMonoAgent agent)
        {

            var traces = config.traceStorage.GetTraces();
            Vector3 eyes = agent.Transform.position;
            Vector3 forward = agent.Transform.forward;
            float timeFloor = brain.HandledNoiseTimestamp;

            GameTrace bestTrace = null;

            foreach (var t in traces)
            {
                if (t.IsExpired || t.Timestamp <= timeFloor) continue;

                bool isValid = false;


                if (IsLoud(t.Type) && Vector3.Distance(eyes, t.Position) <= config.hearingRange)
                {
                    isValid = true;
                }

                else if (Vector3.Distance(eyes, t.Position) <= config.viewRadius)
                {

                    Vector3 dir = (t.Position - eyes).normalized;
                    if (Vector3.Angle(forward, dir) < config.ViewAngle / 2f)
                    {
                        isValid = true;
                    }
                }

                if (isValid)
                {
                    // We found a valid trace. Is it newer than what we found so far?
                    if (bestTrace == null || t.Timestamp > bestTrace.Timestamp)
                    {
                        bestTrace = t;
                    }
                }
            }

            return bestTrace?.Position;
        }

        private bool IsLoud(TraceType t)
        {
            return t == TraceType.Soul_Collection || 
                   t == TraceType.EnviromentNoiseStrong || 
                   t == TraceType.EnviromentNoiseMedium;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}