using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    // [GoapId("Patrol-a07645db-9ba5-40d4-af08-b0b6651d21af")]
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        // Define how close the agent needs to be to the target to complete the action.
        // This should be similar to or slightly greater than the NavMeshAgent's stopping distance.
        private const float StoppingDistance = 1f;

        public override void Created()
        {
        }

        public override void Start(IMonoAgent agent, Data data)
        {
        }

        // This method is called every frame while the action is running.
        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // If the target is somehow null, the action must be stopped so the planner can find a new one.
            if (data.Target == null)
            {
                // CORRECTED LINE: Use 'Stop' instead of 'Failed'
                return ActionRunState.Stop;
            }

            // Calculate the distance to the target in the XZ plane for better accuracy on uneven ground.
            var agentPosition = agent.Transform.position;
            var targetPosition = data.Target.Position;
            agentPosition.y = 0;
            targetPosition.y = 0;
            
            var distance = Vector3.Distance(agentPosition, targetPosition);

            // If the agent is close enough, the action is complete.
            if (distance <= StoppingDistance)
            {
                return ActionRunState.Completed;
            }

            // If the agent is not close enough, tell the planner to continue this action next frame.
            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}