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

        // Public State
        public bool IsStuck { get; private set; }
        
        // Internal
        private SpeedState currentMode;
        private Vector3 lastStuckPos;
        private float stuckTimer;
        
        // "Stop Watch" for zero velocity
        private float zeroVelocityTimer; 

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (config == null) config = GetComponent<MonsterConfig>();

            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
            // Stuck Detection Logic running in background
            if (agent.hasPath && !agent.isStopped)
            {
                // Global "Am I moving at all?" check
                if (Vector3.Distance(transform.position, lastStuckPos) < config.stuckDistanceThreshold)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > config.maxStuckTime) IsStuck = true;
                }
                else
                {
                    lastStuckPos = transform.position;
                    stuckTimer = 0f;
                    IsStuck = false;
                }

                // Precision "Am I blocked?" check
                if (agent.velocity.sqrMagnitude < 0.05f)
                {
                    zeroVelocityTimer += Time.deltaTime;
                }
                else
                {
                    zeroVelocityTimer = 0f;
                }
            }
        }

        public bool GoTo(Vector3 position, SpeedState speedMode)
        {
            IsStuck = false;
            stuckTimer = 0f;
            zeroVelocityTimer = 0f;
            currentMode = speedMode;

            // 1. Sanitize Point (Ensure it's reachable)
            NavMeshHit hit;
            // Try finding a point on mesh
            if (!NavMesh.SamplePosition(position, out hit, 10.0f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[Movement] Target {position} is not on NavMesh.");
                return false;
            }

            position = hit.position;

            // 2. Move
            agent.isStopped = false;
            ApplySpeed(speedMode);
            
            bool pathSet = agent.SetDestination(position);
            
            if (!pathSet) Debug.LogWarning("[Movement] SetDestination failed.");
            
            return pathSet;
        }

        public void Chase(Transform target)
        {
            if (target == null) return;
            // Reset timers
            IsStuck = false; 
            stuckTimer = 0f; 
            
            currentMode = SpeedState.Chase;
            agent.isStopped = false;
            ApplySpeed(SpeedState.Chase);
            agent.SetDestination(target.position);
        }

        public void Stop()
        {
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
            if (agent.pathPending) return false; // Still calculating

            float requiredDist = GetStoppingDistanceForMode(currentMode);
            float threshold = requiredDist + config.arrivalTolerance;

            // 1. Math Check (Horizontal)
            Vector3 agentPos = transform.position; agentPos.y = 0;
            Vector3 targetPos = targetPosition; targetPos.y = 0;
            float dist = Vector3.Distance(agentPos, targetPos);

            if (dist <= threshold) return true;

            // 2. Physics/Obstruction Fail-Safe
            // If we have been trying to move for > 1.0s but Velocity is 0,
            // AND we aren't completely stuck globally (managed by IsStuck),
            // We assume we hit the destination's cover/wall.
            if (zeroVelocityTimer > 1.0f)
            {
                // Only count as arrived if we are somewhat close (within 3x stopping dist)
                // Otherwise it's a "Stuck" situation handled by IsStuck
                if (dist < threshold * 3f) 
                {
                    // Debug.Log("[Movement] Wall hit close to target. Assuming Arrival.");
                    return true;
                }
            }

            return false;
        }

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
    }
}