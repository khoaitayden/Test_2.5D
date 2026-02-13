using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class TauntPlayerAction : GoapActionBase<TauntPlayerAction.Data>
    {
        private MonsterMovement movement;
        private MonsterBrain brain;
        private DrunkMonsterConfig config;
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
            config = agent.GetComponent<DrunkMonsterConfig>();
            navAgent = agent.GetComponent<NavMeshAgent>();
            
            startTime = Time.time;
            movingRight = Random.value > 0.5f; 
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
        private void Move()
        {
            if (brain.CurrentPlayerTarget == null) return;

            float distToPlayer = Vector3.Distance(navAgent.transform.position, brain.CurrentPlayerTarget.position);

            // PHASE 1: APPROACH

            if (distToPlayer > config.idealTauntRange)
            {
                movement.MoveTo(brain.CurrentPlayerTarget.position, config.investigateSpeed);
            }
            // PHASE 2: ORBIT
            else
            {
                PickNewTauntPoint(navAgent.transform);
            }
        }

        private void PickNewTauntPoint(Transform monster)
        {

            float TauntProgress = Mathf.Clamp01((Time.time - startTime) / config.maxChaseTime);


            float currentOrbitRange = Mathf.Lerp(config.maxTauntRange, config.minTauntRange, TauntProgress);
            float currentTauntSpeed = Mathf.Lerp(config.maxTauntSpeed, config.minTauntSpeed, TauntProgress);

            Vector3 toPlayer = brain.CurrentPlayerTarget.position - monster.position;
            Vector3 dirToPlayer = toPlayer.normalized;
            
            Vector3 right = Vector3.Cross(Vector3.up, dirToPlayer).normalized; 
            Vector3 moveDir = movingRight ? right : -right;

            movingRight = !movingRight; 

            Vector3 targetPos = monster.position + (moveDir * currentOrbitRange);

            if (Physics.Raycast(monster.position + Vector3.up, moveDir, 3.0f, config.obstacleLayerMask))
            {
                return; 
            }

            movement.MoveTo(targetPos, currentTauntSpeed);
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