using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float stuckTimeout = 1.0f; 

        // State
        private float standStillTimer;
        private bool isChaseMode;
        private Transform chaseTarget;
        
        // Safety: Don't check arrival immediately after setting a destination
        private float pathSetTime; 

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
        public bool MoveTo(Vector3 position, float speed, float stopDist)
        {
            isChaseMode = false;
            chaseTarget = null;
            standStillTimer = 0f;
            pathSetTime = Time.time;

            agent.speed = speed;
            agent.stoppingDistance = stopDist;
            agent.isStopped = false;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                // 2. CHECK REACHABILITY (The Fix)
                NavMeshPath path = new NavMeshPath();
                agent.CalculatePath(hit.position, path);

                if (path.status == NavMeshPathStatus.PathPartial)
                {
                    // The point is on the NavMesh, but we can't get there (blocked by wall/door)
                    // Solution: Move to the last reachable corner (the wall itself)
                    if (path.corners.Length > 0)
                    {
                        // Go to the point on the wall closest to the sound
                        agent.SetDestination(path.corners[path.corners.Length - 1]);
                        return true;
                    }
                }
                
                // Path is Complete or Valid enough
                return agent.SetDestination(hit.position);
            }

            // Failed to find any navmesh point near the trace
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

            agent.speed = speed;
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

        // --- CHECK ---
        
        public bool HasArrivedOrStuck()
        {
            // SAFETY: Wait 0.25s for path calculation to potentially start/fail
            if (Time.time < pathSetTime + 0.25f) return false;

            if (agent.pathPending) return false;
            
            // If no path after safety time, we are done (or failed)
            if (!agent.hasPath) return true; 

            // 1. Timeout (Hit Wall / Stuck)
            if (standStillTimer > stuckTimeout) return true;

            // 2. NavMesh Arrival
            if (agent.remainingDistance <= agent.stoppingDistance) return true;

            // 3. Partial Path Handling (Stuck at Building Wall)
            // If we can't reach the target (Partial), and we are at the end of the partial path...
            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                if (agent.remainingDistance < 1.0f) return true;
            }

            return false;
        }
    }
}