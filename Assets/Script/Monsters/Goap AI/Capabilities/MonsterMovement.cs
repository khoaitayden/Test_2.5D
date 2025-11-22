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

        // State
        public bool HasArrived { get; private set; }
        public bool IsStuck { get; private set; }
        
        // Tracking Logic
        private Transform targetToFollow;
        private bool isFollowing = false;
        private Vector3 lastStuckPos;
        private float stuckTimer;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (config == null) config = GetComponent<MonsterConfig>();
        }

        // --- API ---

        public bool GoTo(Vector3 position, SpeedState speedMode)
        {
            isFollowing = false;
            targetToFollow = null;
            return MoveInternal(position, speedMode);
        }

        // NEW: Native Chase Mode
        public void Chase(Transform target)
        {
            if (target == null) return;
            isFollowing = true;
            targetToFollow = target;
            MoveInternal(target.position, SpeedState.Chase);
        }

        public void Stop()
        {
            isFollowing = false;
            targetToFollow = null;
            if (agent.isOnNavMesh) 
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        // --- ENGINE ---

        private void Update()
        {
            // 1. Continuous Following
            if (isFollowing && targetToFollow != null && agent.isOnNavMesh)
            {
                // Only update destination if the target has moved significantly (Optimization)
                if (Vector3.SqrMagnitude(agent.destination - targetToFollow.position) > 0.25f)
                {
                    agent.SetDestination(targetToFollow.position);
                }
            }

            // 2. Arrival / Stuck Logic
            if (!agent.pathPending && !agent.isStopped)
            {
                // Standard arrival check
                if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    HasArrived = true;
                }
                else
                {
                    CheckIfStuck();
                }
            }
        }

        private bool MoveInternal(Vector3 pos, SpeedState mode)
        {
            HasArrived = false;
            IsStuck = false;
            stuckTimer = 0f;
            lastStuckPos = transform.position;

            agent.isStopped = false;
            ApplySpeed(mode);
            
            return agent.SetDestination(pos);
        }

        private void ApplySpeed(SpeedState mode)
        {
            switch (mode)
            {
                case SpeedState.Patrol:
                    agent.speed = config.patrolSpeed;
                    agent.acceleration = config.patrolAcceleration;
                    agent.stoppingDistance = 0.5f;
                    break;
                case SpeedState.Chase:
                    agent.speed = config.chaseSpeed;
                    agent.acceleration = config.chaseAcceleration;
                    agent.stoppingDistance = 0.1f; // Don't stop! Hit the player.
                    break;
                case SpeedState.Investigate:
                    agent.speed = config.investigateRushSpeed;
                    agent.acceleration = config.investigateRushAcceleration;
                    agent.stoppingDistance = 0.5f;
                    break;
            }
        }

        private void CheckIfStuck()
        {
            if (Vector3.Distance(transform.position, lastStuckPos) < config.stuckDistanceThreshold)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > config.maxStuckTime)
                {
                    IsStuck = true;
                    // Do not auto-stop during chase; logic might recover
                    if(!isFollowing) Stop(); 
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