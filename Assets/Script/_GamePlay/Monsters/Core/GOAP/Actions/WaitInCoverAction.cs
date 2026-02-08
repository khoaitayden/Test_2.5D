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
        private float playerLastDistance;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            brain = agent.GetComponent<KidnapMonsterBrain>();
            if (brain != null) playerAnchor = brain.PlayerAnchor;
            
            data.startTime = Time.time;
            data.wasSuccessful = false;
            
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                initialPlayerDistance = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
            }
            else
            {
                initialPlayerDistance = float.MaxValue; 
            }
            
            nervousTimer = 0f;
            playerLastDistance=Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // --- 1. PANIC CHECK ---
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                float dist = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
                // if (dist < config.playerComeCloseKidnapDistance)
                // {
                //     data.wasSuccessful = false;
                //     return ActionRunState.Stop; 
                // }

                // --- 2. NERVOUS CHECK ---
                if ((dist < initialPlayerDistance)&&(dist!=playerLastDistance))
                {
                    nervousTimer += Time.deltaTime;
                    playerLastDistance=dist;
                    Debug.Log("NervousTimer: "+ nervousTimer);
                    if (nervousTimer >= config.nervousThreshold)
                    {
                        data.wasSuccessful = false;
                        brain.CanHide=false;
                        Debug.Log("Panic run");
                        return ActionRunState.Stop; 
                    }
                }
                else
                {
                    nervousTimer -= Time.deltaTime;
                }
            }
            
            // --- 3. PATIENCE CHECK ---
            if (Time.time > data.startTime + config.hideBehindCoverDuration)
            {
                data.wasSuccessful = true;
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data) 
        { 
            Debug.Log("Ending");
            if (data.wasSuccessful)
            {
                brain?.OnSafetyAchieved();
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
            public bool wasSuccessful; 
        }
    }
}