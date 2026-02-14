using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float stuckTimeout = 1.0f;

        public float AnimationSpeedFactor { get; set; } = 1.0f; 

        // State
        private float standStillTimer;
        private bool isChaseMode;
        private Transform chaseTarget;
        private float pathSetTime;
        private MonsterConfigBase config;
        
        private float targetSpeed; 
    
        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            config = GetComponent<MonsterConfigBase>();
            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
            agent.speed = targetSpeed * AnimationSpeedFactor;

            if (isChaseMode && chaseTarget != null)
            {
                if (Vector3.SqrMagnitude(agent.destination - chaseTarget.position) > 1.0f)
                    agent.SetDestination(chaseTarget.position);
            }

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

            Vector3 finalDestination = targetPos;
            bool foundValidPoint = false;

            float[] searchRadii = new float[] { 
                5.0f, 
                config.traceNavMeshSnapRadius, 
                config.traceNavMeshFallbackRadius,
                50.0f 
            };

            NavMeshHit hit;
            for (int i = 0; i < searchRadii.Length; i++)
            {
                if (NavMesh.SamplePosition(targetPos, out hit, searchRadii[i], agent.areaMask))
                {
                    finalDestination = hit.position;
                    foundValidPoint = true;
                    break;
                }
            }

            if (!foundValidPoint)
            {
                Debug.LogWarning("[MonsterMovement] Target is completely unreachable/off-mesh.");
                agent.ResetPath();
                return false; 
            }

            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(finalDestination, path);

            // if (path.status == NavMeshPathStatus.PathInvalid)
            // {
            //     agent.ResetPath();
            //     return false;
            // }

            if (path.status == NavMeshPathStatus.PathPartial && path.corners.Length > 0)
            {
                finalDestination = path.corners[path.corners.Length - 1];
            }

            if (Vector3.Distance(transform.position, finalDestination) < 1.0f)
            {
                return false;
            }

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