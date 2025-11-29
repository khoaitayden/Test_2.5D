using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float stuckTimeout = 1.0f; // If stuck for 1s, force arrival

        // State
        private float standStillTimer;
        private bool isChaseMode;
        private Transform chaseTarget;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
            // Chase Logic
            if (isChaseMode && chaseTarget != null)
            {
                if (Vector3.SqrMagnitude(agent.destination - chaseTarget.position) > 1.0f)
                    agent.SetDestination(chaseTarget.position);
            }

            // Stuck Logic
            if (agent.hasPath && !agent.isStopped)
            {
                if (agent.velocity.sqrMagnitude < 0.1f) standStillTimer += Time.deltaTime;
                else standStillTimer = 0f;
            }
            else
            {
                standStillTimer = 0f;
            }
        }

        // --- API ---

        public void MoveTo(Vector3 position, float speed, float stopDist)
        {
            isChaseMode = false;
            chaseTarget = null;
            standStillTimer = 0f;

            agent.speed = speed;
            agent.stoppingDistance = stopDist;
            agent.isStopped = false;

            // Sanitize Point
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        public void Chase(Transform target, float speed)
        {
            if (target == null) return;
            if (isChaseMode && chaseTarget == target) return;

            isChaseMode = true;
            chaseTarget = target;
            standStillTimer = 0f;

            agent.speed = speed;
            agent.stoppingDistance = 0.5f; // Kill distance
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        public void Stop()
        {
            isChaseMode = false;
            chaseTarget = null;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        // --- CHECK ---
        
        public bool HasArrivedOrStuck()
        {
            if (agent.pathPending) return false;
            if (!agent.hasPath) return true; // No path = Arrived

            // 1. Timeout (Hit Wall)
            if (standStillTimer > stuckTimeout) return true;

            // 2. NavMesh Arrival
            if (agent.remainingDistance <= agent.stoppingDistance) return true;

            return false;
        }
    }
}