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
        private MonsterConfig config;
        
        // Store the speed requested by GOAP so we can modify it
        private float targetSpeed; 
    
        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            config = GetComponent<MonsterConfig>();
            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
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

        public bool MoveTo(Vector3 targetPos, float speed)
        {
            isChaseMode = false;
            chaseTarget = null;
            standStillTimer = 0f;
            pathSetTime = Time.time;
            targetSpeed = speed;
            agent.isStopped = false;
            AnimationSpeedFactor = 1.0f;

            // Get Settings from Config or use Defaults
            float snapRadius = config.traceNavMeshSnapRadius;
            float fallbackRadius = config.traceNavMeshFallbackRadius;

            NavMeshHit hit;
            Vector3 finalDestination = Vector3.zero;
            bool foundValidPoint = false;

            // 1. Try Precise Snap
            if (NavMesh.SamplePosition(targetPos, out hit, snapRadius, NavMesh.AllAreas))
            {
                finalDestination = hit.position;
                foundValidPoint = true;
            }
            // 2. Try Fallback Snap (Wider search)
            else if (NavMesh.SamplePosition(targetPos, out hit, fallbackRadius, NavMesh.AllAreas))
            {
                finalDestination = hit.position;
                foundValidPoint = true;
            }

            if (!foundValidPoint)
            {
                agent.ResetPath();
                return false; // Point is off the map
            }

            // 3. CHECK REACHABILITY (Prevent Spinning)
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(finalDestination, path);

            if (path.status == NavMeshPathStatus.PathInvalid)
            {
                agent.ResetPath();
                return false;
            }

            if (path.status == NavMeshPathStatus.PathPartial)
            {
                if (path.corners.Length > 0)
                {
                    finalDestination = path.corners[path.corners.Length - 1];
                }
            }

            // 6. Execute
            return agent.SetDestination(finalDestination);
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
                if (agent.remainingDistance < 2.0f) return true;
            }

            return false;
        }
    }
}