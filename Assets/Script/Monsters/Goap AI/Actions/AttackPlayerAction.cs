using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class AttackPlayerAction : GoapActionBase<AttackPlayerAction.Data>
    {
        private MonsterTouchSensor touchSensor;
        private MonsterMovement movement;
        
        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            if (touchSensor == null) touchSensor = agent.GetComponent<MonsterTouchSensor>();

            // DETERMINE TARGET TRANSFORM
            Transform chaseTarget = null;

            if (data.Target is TransformTarget dynamicTarget)
            {
                chaseTarget = dynamicTarget.Transform;
            }
            else
            {
                // Fallback: Find by tag if data is broken/static
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) chaseTarget = playerObj.transform;
            }

            // ACTIVATE MOVEMENT MODE
            if (chaseTarget != null)
            {
                movement.Chase(chaseTarget);
            }
            else
            {
                Debug.LogWarning("[Attack] No live target found to chase!");
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // This logic runs every frame, but we don't need to handle movement updates anymore.
            // The MonsterMovement.Update() loop is doing that natively now.
            
            if (touchSensor != null && touchSensor.IsTouchingPlayer)
            {
                return ActionRunState.Completed;
            }
            
            return ActionRunState.Continue;
        }
        
        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}