using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class StalkPlayerAction : GoapActionBase<StalkPlayerAction.Data>
    {
        private MonsterMovement movement;
        private MonsterBrain brain;
        private MonsterConfig config;
        private NavMeshAgent navAgent;
        
        private float startTime;
        private bool movingRight = true;
        private NavMeshPath _tempPath;

        public override void Created() 
        {
            _tempPath = new NavMeshPath();
        }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            brain = agent.GetComponent<MonsterBrain>();
            config = agent.GetComponent<MonsterConfig>();
            navAgent = agent.GetComponent<NavMeshAgent>();
            
            startTime = Time.time;
            movingRight = Random.value > 0.5f; 
            
            // Initial move is always to try and get in range
            Move(); 
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (CheckIfPlayerIsReachable())
            {
                return ActionRunState.Completed; // Transition to Attack
            }

            if (Time.time > startTime + config.maxChaseTime)
            {
                brain.OnMovementStuck(); 
                return ActionRunState.Completed;
            }

            if (brain.CurrentPlayerTarget == null) return ActionRunState.Stop;

            // --- THE CORE LOGIC: Re-evaluate move every time we arrive ---
            if (movement.HasArrivedOrStuck())
            {
                Move();
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        // --- NEW: Central Move Logic ---
        private void Move()
        {
            if (brain.CurrentPlayerTarget == null) return;

            float distToPlayer = Vector3.Distance(navAgent.transform.position, brain.CurrentPlayerTarget.position);

            // PHASE 1: APPROACH
            // If we are further than the ideal stalking range...
            if (distToPlayer > config.idealStalkingRange)
            {
                // ...move towards the player.
                // MonsterMovement will handle snapping to the nearest valid point (the wall).
                movement.MoveTo(brain.CurrentPlayerTarget.position, config.investigateSpeed);
            }
            // PHASE 2: ORBIT
            // If we are close enough, start pacing.
            else
            {
                PickNewStalkPoint(navAgent.transform);
            }
        }

        private void PickNewStalkPoint(Transform monster)
        {
            // Calculate Progress (0.0 to 1.0)
            float stalkProgress = Mathf.Clamp01((Time.time - startTime) / config.maxChaseTime);

            // Interpolate using Config variables
            float currentOrbitRange = Mathf.Lerp(config.maxStalkRange, config.minStalkRange, stalkProgress);
            float currentStalkSpeed = Mathf.Lerp(config.maxStalkSpeed, config.minStalkSpeed, stalkProgress);

            // Calculate Target Position
            Vector3 toPlayer = brain.CurrentPlayerTarget.position - monster.position;
            Vector3 dirToPlayer = toPlayer.normalized;
            
            Vector3 right = Vector3.Cross(Vector3.up, dirToPlayer).normalized; 
            Vector3 moveDir = movingRight ? right : -right;

            // Switch direction for next time
            movingRight = !movingRight; 

            Vector3 targetPos = monster.position + (moveDir * currentOrbitRange);

            if (Physics.Raycast(monster.position + Vector3.up, moveDir, 3.0f, config.obstacleLayerMask))
            {
                // Blocked, try other side next time (will trigger on next HasArrived check)
                return; 
            }

            movement.MoveTo(targetPos, currentStalkSpeed);
        }
        
        private bool CheckIfPlayerIsReachable()
        {
            if (brain.CurrentPlayerTarget == null) return false;
            Vector3 targetPos = brain.CurrentPlayerTarget.position;
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                navAgent.CalculatePath(hit.position, _tempPath);
                if (_tempPath.status == NavMeshPathStatus.PathComplete)
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