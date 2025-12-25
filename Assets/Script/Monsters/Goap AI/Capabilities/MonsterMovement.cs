using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float stuckTimeout = 1.0f;

        // --- NEW: Connection to Climber ---
        // This allows the animation to throttle the speed
        public float AnimationSpeedFactor { get; set; } = 1.0f; 

        // State
        private float standStillTimer;
        private bool isChaseMode;
        private Transform chaseTarget;
        private float pathSetTime;
        
        // Store the speed requested by GOAP so we can modify it
        private float targetSpeed; 

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
            // --- NEW: Apply Rhythm ---
            // We multiply the GOAP desired speed by the Animation Factor (0 to 1)
            // If the climber says "Reach", factor is 0, agent stops.
            // If climber says "Pull", factor is 1, agent moves.
            agent.speed = targetSpeed * AnimationSpeedFactor;

            // Chase Logic
            if (isChaseMode && chaseTarget != null)
            {
                if (Vector3.SqrMagnitude(agent.destination - chaseTarget.position) > 1.0f)
                    agent.SetDestination(chaseTarget.position);
            }

            // Stuck Logic
            // Note: We check if factor > 0.1 to avoid detecting "Stuck" when we are just pausing for animation
            if (agent.hasPath && !agent.isStopped && AnimationSpeedFactor > 0.1f)
            {
                if (agent.velocity.sqrMagnitude < 0.1f) standStillTimer += Time.deltaTime;
                else standStillTimer = 0f;
            }
            else
            {
                standStillTimer = 0f;
            }
        }

        public bool MoveTo(Vector3 position, float speed, float stopDist)
        {
            isChaseMode = false;
            chaseTarget = null;
            standStillTimer = 0f;
            pathSetTime = Time.time;

            targetSpeed = speed; // Store desired speed
            agent.stoppingDistance = stopDist; 
            agent.isStopped = false;
            
            // Reset factor so we start moving immediately
            AnimationSpeedFactor = 1.0f; 

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                agent.CalculatePath(hit.position, path);

                if (path.status == NavMeshPathStatus.PathPartial)
                {
                    if (path.corners.Length > 0)
                    {
                        agent.SetDestination(path.corners[path.corners.Length - 1]);
                        return true;
                    }
                }
                return agent.SetDestination(hit.position);
            }

            agent.ResetPath();
            return false;
        }

        public void Chase(Transform target, float speed)
        {
            if (target == null) return;
            if (isChaseMode && chaseTarget == target) return;

            isChaseMode = true;
            chaseTarget = target;
            standStillTimer = 0f;
            pathSetTime = Time.time;

            targetSpeed = speed; // Store desired speed
            agent.stoppingDistance = 0.5f; 
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        public void Stop()
        {
            isChaseMode = false;
            chaseTarget = null;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        public bool HasArrivedOrStuck()
        {
            if (Time.time < pathSetTime + 0.25f) return false;
            if (agent.pathPending) return false;
            if (!agent.hasPath) return true; 
            if (standStillTimer > stuckTimeout) return true;
            if (agent.remainingDistance <= agent.stoppingDistance) return true;

            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                if (agent.remainingDistance < 1.0f) return true;
            }

            return false;
        }
    }
}