using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfigBase config;
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfigBase>();
            brain = agent.GetComponent<MonsterBrain>();

            if (data.Target != null)
            {
                movement.MoveTo(data.Target.Position, config.patrolSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Target == null) return ActionRunState.Stop;

            // --- INTERRUPT LOGIC (Noise) ---
            if (CheckForNoise(agent.Transform.position))
            {
                return ActionRunState.Completed; 
            }

            if (movement.HasArrivedOrStuck())
            {
                
                if (Vector3.Distance(agent.Transform.position, data.Target.Position) > 3.0f)
                {
                    Debug.LogWarning("[Patrol] Stuck far from target. Fleeing.");
                    if (brain != null) brain.OnMovementStuck();
                    return ActionRunState.Stop;
                }
                Debug.Log("Arrived on patrol");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        private bool CheckForNoise(Vector3 pos)
        {
            var traces = config.traceStorage.GetTraces();
            float timeFloor = brain.HandledNoiseTimestamp;

            for (int i = traces.Count - 1; i >= 0; i--)
            {
                var t = traces[i];
                if (t.IsExpired || t.Timestamp <= timeFloor) continue;

                bool isLoud = t.Type == TraceType.Soul_Collection || 
                              t.Type == TraceType.EnviromentNoiseStrong ||
                              t.Type == TraceType.EnviromentNoiseMedium;

                if (isLoud && Vector3.Distance(pos, t.Position) <= config.hearingRange)
                {
                    return true;
                }
            }
            return false;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}