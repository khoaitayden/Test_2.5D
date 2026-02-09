using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class WaitInCoverAction : GoapActionBase<WaitInCoverAction.Data>
    {
        private KidnapMonsterConfig config;
        private TransformAnchorSO playerAnchor;
        private KidnapMonsterBrain brain;
        private MonsterMovement movement;

        private float initialPlayerDistance;
        private float nervousTimer; 
        private float playerLastDistance;
        private Vector3 currentCoverTreePosition; 

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            brain = agent.GetComponent<KidnapMonsterBrain>();
            movement = agent.GetComponent<MonsterMovement>(); 
            
            if (brain != null) playerAnchor = brain.PlayerAnchor;
            
            data.startTime = Time.time;
            data.wasSuccessful = false;
            
            if (data.Target != null)
            {
                currentCoverTreePosition = FindTreeCenter(agent.Transform.position);
            }

            if (playerAnchor != null && playerAnchor.Value != null)
            {
                initialPlayerDistance = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
                playerLastDistance = initialPlayerDistance; 
            }
            
            nervousTimer = 0f;
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (playerAnchor != null && playerAnchor.Value != null)
            {
                UpdateHidingPosition(agent);

                // --- PANIC CHECKS ---
                float dist = Vector3.Distance(agent.Transform.position, playerAnchor.Value.position);
                if (dist < config.playerComeCloseKidnapDistance || (dist < playerLastDistance - 0.05f && (nervousTimer += Time.deltaTime) >= config.nervousThreshold))
                {
                    data.wasSuccessful = false;
                    if(brain != null) brain.CanHide = false;
                    return ActionRunState.Stop; 
                }
                else
                {
                    nervousTimer -= Time.deltaTime;
                    if (nervousTimer < 0) nervousTimer = 0;
                }
                
                playerLastDistance = dist;
            }
            
            // --- PATIENCE CHECK ---
            if (Time.time > data.startTime + config.hideBehindCoverDuration)
            {
                data.wasSuccessful = true;
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        private void UpdateHidingPosition(IMonoAgent agent)
        {
            if (currentCoverTreePosition == Vector3.zero) return;

            Vector3 playerPos = playerAnchor.Value.position;
            Vector3 shadowDirection = (currentCoverTreePosition - playerPos).normalized;

            Vector3 idealHideSpot = currentCoverTreePosition + (shadowDirection * config.hideDistanceBehindTree);

            movement.MoveTo(idealHideSpot, config.rotateHidingSpeed);
            
            Vector3 lookDir = (playerPos - agent.Transform.position).normalized;
            lookDir.y = 0;
            if(lookDir != Vector3.zero) 
                agent.Transform.rotation = Quaternion.Slerp(agent.Transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * config.hideLookSpeed);
        }

        private Vector3 FindTreeCenter(Vector3 myPos)
            {
                Vector3 toPlayer = playerAnchor.Value.position - myPos;

                Collider[] hits = Physics.OverlapSphere(myPos, config.treeDetectionRadius, config.obstacleLayerMask);
                
                // Return closest tree
                float closest = float.MaxValue;
                Vector3 bestTree = Vector3.zero;
                
                foreach(var hit in hits)
                {
                    float d = Vector3.Distance(myPos, hit.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        bestTree = hit.bounds.center;
                        bestTree.y = myPos.y;
                    }
                }
                
                return bestTree;
            }

        public override void End(IMonoAgent agent, Data data) 
        { 
            movement.Stop();
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