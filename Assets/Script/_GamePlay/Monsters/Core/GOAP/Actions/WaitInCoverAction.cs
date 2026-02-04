using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class WaitInCoverAction : GoapActionBase<WaitInCoverAction.Data>
    {
        private KidnapMonsterConfig config;
        private TransformAnchorSO playerAnchor;
        private KidnapMonsterBrain brain;

        private float initialPlayerDistance;
        private float nervousTimer;
        private const float NERVOUS_THRESHOLD = 2.0f; 

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            brain = agent.GetComponent<KidnapMonsterBrain>();
            if (brain != null) playerAnchor = brain.PlayerAnchor;
            
            data.startTime = Time.time;
            
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                initialPlayerDistance = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
            }
            else
            {
                initialPlayerDistance = float.MaxValue; 
            }
            
            nervousTimer = 0f;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // --- 1. PANIC CHECK (Too Close) ---
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                float dist = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
                if (dist < config.playerComeCloseFleeDistance)
                {
                    return ActionRunState.Stop; // Fail action -> Replan to Flee
                }

                // --- 2. NERVOUS CHECK (Approaching) ---
                if (dist < initialPlayerDistance)
                {
                    nervousTimer += Time.deltaTime;
                    if (nervousTimer >= NERVOUS_THRESHOLD)
                    {
                        return ActionRunState.Stop; // Fail action -> Replan to Flee
                    }
                }
                else
                {
                    nervousTimer = 0f;
                }
            }
            
            // 3. PATIENCE CHECK (Timeout)
            if (Time.time > data.startTime + 5.0f)
            {
                return ActionRunState.Completed; // Success, we are "Safe"
            }

            return ActionRunState.Continue;
        }

        // --- THE FIX ---
        // This is called ONLY if Perform returns ActionRunState.Completed
        public override void End(IMonoAgent agent, Data data) 
        { 
            // We waited successfully
            brain?.OnSafetyAchieved();
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}