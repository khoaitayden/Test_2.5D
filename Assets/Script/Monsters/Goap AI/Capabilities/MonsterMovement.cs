using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        public enum SpeedState { Patrol, Chase, Investigate }

        [Header("Components")]
        [SerializeField] private MonsterConfig config;
        [SerializeField] private NavMeshAgent agent;

        // Internal State
        public bool IsStuck { get; private set; }
        private Vector3 lastStuckPos;
        private float stuckTimer;
        
        // Tracking
        private SpeedState currentMode;
        private Transform targetToFollow; // For Chasing
        private bool isFollowing = false;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (config == null) config = GetComponent<MonsterConfig>();

            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
            // 1. Handle Continuous Chasing
            if (isFollowing && targetToFollow != null && agent.isOnNavMesh)
            {
                // Update destination if target moved > 0.5m
                if (Vector3.SqrMagnitude(agent.destination - targetToFollow.position) > 0.5f)
                {
                    agent.SetDestination(targetToFollow.position);
                }
            }

            // 2. Handle Stuck Detection
            if (agent.hasPath && !agent.isStopped && agent.remainingDistance > 1.0f)
            {
                CheckStuck();
            }
        }

        // --- COMMANDS ---

        public bool GoTo(Vector3 position, SpeedState speedMode)
        {
            // Disable chasing
            isFollowing = false;
            targetToFollow = null;
            
            IsStuck = false;
            stuckTimer = 0f;
            lastStuckPos = transform.position;
            currentMode = speedMode;

            // Sanitize
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                position = hit.position;
            }

            agent.isStopped = false;
            ApplySpeed(speedMode);

            return agent.SetDestination(position);
        }

        // RESTORED: The Chase Method
        public void Chase(Transform target)
        {
            if (target == null) return;
            
            // Enable chasing
            isFollowing = true;
            targetToFollow = target;
            
            IsStuck = false;
            stuckTimer = 0f;
            currentMode = SpeedState.Chase;

            agent.isStopped = false;
            ApplySpeed(SpeedState.Chase);
            
            // Set initial move
            agent.SetDestination(target.position);
        }

        public void Stop()
        {
            isFollowing = false;
            targetToFollow = null;
            
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        // --- THE SOURCE OF TRUTH ---

        public bool HasReached(Vector3 targetPosition)
        {
            float requiredDist = GetStoppingDistanceForMode(currentMode);
            float threshold = requiredDist + config.arrivalTolerance;

            // Flatten Positions (Ignore Height)
            Vector3 agentPos = transform.position;
            agentPos.y = 0;
            Vector3 targetPos = targetPosition;
            targetPos.y = 0;

            float dist = Vector3.Distance(agentPos, targetPos);

            if (dist <= threshold) return true;

            // Fail-Safe: Velocity Check
            if (agent.hasPath && !agent.pathPending)
            {
                // If stopped moving and reasonably close
                if (agent.remainingDistance <= threshold && agent.velocity.sqrMagnitude < 0.05f)
                {
                    return true;
                }
            }

            return false;
        }

        // --- HELPERS ---

        private void ApplySpeed(SpeedState mode)
        {
            switch (mode)
            {
                case SpeedState.Patrol:
                    agent.speed = config.patrolSpeed;
                    agent.acceleration = config.patrolAcceleration;
                    break;
                case SpeedState.Chase:
                    agent.speed = config.chaseSpeed;
                    agent.acceleration = config.chaseAcceleration;
                    break;
                case SpeedState.Investigate:
                    agent.speed = config.investigateRushSpeed;
                    agent.acceleration = config.investigateRushAcceleration;
                    break;
            }
        }

        private float GetStoppingDistanceForMode(SpeedState mode)
        {
            switch (mode)
            {
                case SpeedState.Patrol: return config.patrolStoppingDistance;
                case SpeedState.Chase: return config.chaseStoppingDistance;
                case SpeedState.Investigate: return config.investigateStoppingDistance;
                default: return 1.0f;
            }
        }

        private void CheckStuck()
        {
            if (Vector3.Distance(transform.position, lastStuckPos) < config.stuckDistanceThreshold)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > config.maxStuckTime)
                {
                    IsStuck = true;
                }
            }
            else
            {
                lastStuckPos = transform.position;
                stuckTimer = 0f;
            }
        }
    }
}