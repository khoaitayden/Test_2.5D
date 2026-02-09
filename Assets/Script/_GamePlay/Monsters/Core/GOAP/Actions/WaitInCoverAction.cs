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
                playerLastDistance = initialPlayerDistance; 
            }
            else
            {
                initialPlayerDistance = float.MaxValue; 
                playerLastDistance = float.MaxValue;
            }
            
            nervousTimer = 0f;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                // --- 0. ROTATE TO FACE PLAYER (NEW) ---
                RotateTowardsPlayer(agent);

                // --- 1. PANIC CHECK ---
                float dist = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
                if (dist < config.playerComeCloseKidnapDistance)
                {
                    data.wasSuccessful = false;
                    return ActionRunState.Stop; 
                }

                // --- 2. NERVOUS CHECK ---
                if (dist < initialPlayerDistance && dist < playerLastDistance - 0.05f) 
                {
                    nervousTimer += Time.deltaTime;
                    // Debug.Log($"Nervous: {nervousTimer}");
                    
                    if (nervousTimer >= config.nervousThreshold)
                    {
                        data.wasSuccessful = false;
                        if(brain != null) brain.CanHide = false;
                        Debug.Log("Panic run");
                        return ActionRunState.Stop; 
                    }
                }
                else
                {
                    nervousTimer -= Time.deltaTime;
                    if (nervousTimer < 0) nervousTimer = 0;
                }
                
                playerLastDistance = dist;
            }
            
            // --- 3. PATIENCE CHECK ---
            if (Time.time > data.startTime + config.hideBehindCoverDuration)
            {
                data.wasSuccessful = true;
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        private void RotateTowardsPlayer(IMonoAgent agent)
        {
            Vector3 direction = (playerAnchor.Value.position - agent.Transform.position).normalized;
            direction.y = 0; // Keep upright, don't look up/down at feet

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                agent.Transform.rotation = Quaternion.Slerp(agent.Transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }

        public override void End(IMonoAgent agent, Data data) 
        { 
            Debug.Log("Ending Wait");
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