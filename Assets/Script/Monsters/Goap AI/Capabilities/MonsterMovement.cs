using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        public enum SpeedState { Patrol, Chase, Investigate }

        [Header("DEBUGGING")]
        public bool debugMode = false;

        [Header("Components")]
        [SerializeField] private MonsterConfig config;
        [SerializeField] private NavMeshAgent agent;

        // Logic
        private float standStillTimer;
        private bool isFollowing;
        private Transform targetToFollow;
        private float debugTimer;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (config == null) config = GetComponent<MonsterConfig>();
            agent.autoBraking = false; 
        }

        private void Update()
        {
            // 1. Continuous Chase Logic
            if (isFollowing && targetToFollow != null)
            {
                // Only update if target moved significantly to save CPU
                if (Vector3.SqrMagnitude(agent.destination - targetToFollow.position) > 1.0f)
                {
                    // FIX: Sample the target position to ensure it's valid
                    NavMeshHit hit;
                    Vector3 targetPos = targetToFollow.position;
                    if (NavMesh.SamplePosition(targetPos, out hit, 5.0f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
            }

            // 2. Stand Still Timer
            if (agent.hasPath && !agent.isStopped)
            {
                if (agent.velocity.sqrMagnitude < 0.1f)
                {
                    standStillTimer += Time.deltaTime;
                }
                else
                {
                    standStillTimer = 0f;
                }
            }
            else
            {
                standStillTimer = 0f;
            }

            // Debugging
            if (debugMode)
            {
                debugTimer += Time.deltaTime;
                if (debugTimer > 0.5f)
                {
                    debugTimer = 0f;
                    Debug.Log($"[Movement] Mode: {(isFollowing ? "CHASE" : "GOTO")} | Vel: {agent.velocity.magnitude:F2} | Dist: {agent.remainingDistance:F2} | PathStatus: {agent.pathStatus}");
                }
            }
        }

        // --- API ---

        public void GoTo(Vector3 position, SpeedState speedMode)
        {
            ResetState();
            ApplyConfig(speedMode);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.ResetPath();
            }
        }

        public void Chase(Transform target)
        {
            Debug.Log("Chase init");
            if (target == null) {
                return;
            };
            
            // Prevent spamming config/reset if already tracking
            if (isFollowing && targetToFollow == target) return;

            ResetState();
            isFollowing = true;
            targetToFollow = target;

            ApplyConfig(SpeedState.Chase);
            
            // Initial Move
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                bool result = agent.SetDestination(hit.position);
                if (debugMode) Debug.Log($"[Movement] Start Chase. Path Found: {result}");
            }
        }

        public void Stop()
        {
            ResetState();
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        // --- CHECK ---
        public bool HasArrivedOrStuck()
        {
            if (agent.pathPending) return false;
            if (!agent.hasPath) return true;

            // Timeout
            if (standStillTimer > config.standStillTimeout) return true;

            // Invalid Path
            if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                if (agent.remainingDistance < config.baseStoppingDistance) return true;
                if (float.IsInfinity(agent.remainingDistance)) return true;
            }

            // Arrival
            if (agent.remainingDistance <= agent.stoppingDistance) return true;

            return false;
        }

        private void ResetState()
        {
            isFollowing = false;
            targetToFollow = null;
            standStillTimer = 0f;
            if (agent.isOnNavMesh) agent.isStopped = false;
        }

        private void ApplyConfig(SpeedState mode)
        {
            agent.stoppingDistance = config.baseStoppingDistance;

            switch (mode)
            {
                case SpeedState.Patrol:
                    agent.speed = config.patrolSpeed;
                    agent.acceleration = config.patrolAcceleration;
                    break;
                case SpeedState.Chase:
                    agent.speed = config.chaseSpeed;
                    agent.acceleration = config.chaseAcceleration;
                    agent.stoppingDistance = 0f; // Kill distance
                    break;
                case SpeedState.Investigate:
                    agent.speed = config.investigateRushSpeed;
                    agent.acceleration = config.investigateRushAcceleration;
                    break;
            }
        }
    }
}