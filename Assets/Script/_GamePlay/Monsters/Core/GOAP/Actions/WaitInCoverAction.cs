using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class WaitInCoverAction : GoapActionBase<WaitInCoverAction.Data>
    {
        private KidnapMonsterConfig kidnapConfig;
        private KidnapMonsterBrain brain;
        private TransformAnchorSO playerAnchor;
        
        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cast config safely
            kidnapConfig = agent.GetComponent<MonsterConfigBase>() as KidnapMonsterConfig;
            brain = agent.GetComponent<KidnapMonsterBrain>();
            Debug.Log("start waiting");
            if (brain != null) playerAnchor = brain.PlayerAnchor;
            
            data.startTime = Time.time;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // 1. Safety Check: Panic if player rushes us
            if (playerAnchor != null && playerAnchor.Value != null && kidnapConfig != null)
            {
                float dist = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
                if (dist < kidnapConfig.playerComeCloseFleeDistance)
                {
                    return ActionRunState.Stop; 
                }
            }

            // 2. Patience Timer (5 seconds)
            if (Time.time > data.startTime + kidnapConfig.hideBehindCoverDuration)
            {
                return ActionRunState.Completed; 
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data) 
        { 
            var brain = agent.GetComponent<KidnapMonsterBrain>();
            if (brain != null)
            {
                // We are done waiting. Reset everything to go back to Kidnap mode.
                brain.OnHideComplete();
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}