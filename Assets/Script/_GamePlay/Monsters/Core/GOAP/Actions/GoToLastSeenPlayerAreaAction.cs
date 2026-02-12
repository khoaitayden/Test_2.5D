using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerAreaAction : GoapActionBase<GoToLastSeenPlayerAreaAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfigBase config;
        private MonsterBrain brain;
        private bool initFailed;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfigBase>();
            brain = agent.GetComponent<MonsterBrain>();
            initFailed = false;
            
            if (data.Target != null)
            {
                bool success = movement.MoveTo(data.Target.Position, config.investigateSpeed);
                
                if (!success)
                {
                    Debug.LogWarning($"[GoTo] Path Failed.");

                    brain?.OnMovementStuck();

                }
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (initFailed || data.Target == null) return ActionRunState.Stop;
            
            if (movement.HasArrivedOrStuck())
            {
                brain?.OnArrivedAtSuspiciousLocation();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data) { movement.Stop(); }
        public class Data : IActionData { public ITarget Target { get; set; } }
    }
}