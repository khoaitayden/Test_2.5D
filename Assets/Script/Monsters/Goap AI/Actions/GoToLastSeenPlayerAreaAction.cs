using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class GoToLastSeenPlayerAreaAction : GoapActionBase<GoToLastSeenPlayerAreaAction.Data>
    {
        private MonsterMovement movement;
        private MonsterConfig config;
        private MonsterBrain brain;
        private bool initFailed;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();
            initFailed = false;
            
            if (data.Target != null)
            {
                // Try to move
                bool success = movement.MoveTo(data.Target.Position, config.investigateSpeed, config.stoppingDistance);
                
                if (!success)
                {
                    Debug.LogWarning($"[GoTo] Path Failed. Resetting investigation to Current Location.");
                    
                    // Instead of failing completely (going to Patrol), 
                    // we tell the Brain to search HERE.
                    brain?.OnMovementStuck(); // Re-use the same logic!
                    
                    // We don't need to continue this action since we are already "Here"
                    // The planner will switch to SearchSurroundings next frame.
                }
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (initFailed || data.Target == null) return ActionRunState.Stop;
            
            if (movement.HasArrivedOrStuck())
            {
                // If stuck trying to reach the investigation area
                if (Vector3.Distance(agent.Transform.position, data.Target.Position) > 5.0f)
                {
                    brain?.OnMovementStuck();
                    return ActionRunState.Stop;
                }
                
                brain?.OnArrivedAtSuspiciousLocation();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data) { movement.Stop(); }
        public class Data : IActionData { public ITarget Target { get; set; } }
    }
}