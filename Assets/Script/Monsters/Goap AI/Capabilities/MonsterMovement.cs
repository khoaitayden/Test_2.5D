using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMovement : MonoBehaviour
    {
        public enum SpeedState { Patrol, Chase, Investigate }

        [Header("DEBUGGING")]
        public bool debugMode = true; // Check this in Inspector!

        [Header("Components")]
        [SerializeField] private MonsterConfig config;
        [SerializeField] private NavMeshAgent agent;

        // Internal Logic
        private float standStillTimer; 
        private Vector3 positionAtLastCheck;
        private float nextCheckTime;
        private float movementStartTime;
        private float debugLogTimer;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (config == null) config = GetComponent<MonsterConfig>();

            agent.autoBraking = false; 
            agent.autoRepath = true;
        }

        private void Update()
        {
            // --- MOVEMENT CHECK LOGIC ---
            if (agent.hasPath && !agent.isStopped)
            {
                // Check position every 0.2 seconds
                if (Time.time > nextCheckTime)
                {
                    float movedDist = Vector3.Distance(transform.position, positionAtLastCheck);

                    // If we moved less than the threshold (e.g. 0.2m)
                    if (movedDist < config.minEffectiveMovement)
                    {
                        // And we have been trying to move for at least 0.5s
                        if (Time.time > movementStartTime + 0.5f)
                        {
                            standStillTimer += 0.2f; 
                        }
                    }
                    else
                    {
                        // We moved enough! Reset.
                        if (standStillTimer > 0 && debugMode) Debug.Log($"[RESET] Moved {movedDist:F2}m. Timer reset.");
                        standStillTimer = 0f;
                    }

                    positionAtLastCheck = transform.position;
                    nextCheckTime = Time.time + 0.2f;
                }
            }
            else
            {
                standStillTimer = 0f;
            }

            // --- DETAILED DEBUGGING LOG ---
            if (debugMode)
            {
                debugLogTimer += Time.deltaTime;
                if (debugLogTimer > 0.5f)
                {
                    debugLogTimer = 0f;
                    PrintDebugStatus();
                }
            }
        }

        private void PrintDebugStatus()
        {
            string status = "";

            if (!agent.hasPath) status = "NO_PATH";
            else if (agent.isStopped) status = "STOPPED_API";
            else if (agent.pathPending) status = "CALCULATING";
            else status = "MOVING";

            float remDist = agent.hasPath ? agent.remainingDistance : 0f;
            float stopDist = agent.stoppingDistance;
            
            // This is the condition HasReached uses:
            bool distCheck = remDist <= config.baseStoppingDistance;
            bool timerCheck = standStillTimer > config.standStillTime;

            string color = (distCheck || timerCheck) ? "<color=green>" : "<color=red>";
            string endColor = "</color>";

            Debug.Log($"[MonsterMovement] Status: {status} | " +
                      $"Dist: {remDist:F2} / {config.baseStoppingDistance} | " +
                      $"Timer: {standStillTimer:F1} / {config.standStillTime} | " +
                      $"Result: {color}ARRIVED? {(distCheck || timerCheck)}{endColor}");
        }

        // --- API ---

        public bool GoTo(Vector3 position, SpeedState speedMode)
        {
            ResetTrackers();

            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 10.0f, NavMesh.AllAreas))
            {
                position = hit.position;
            }

            ApplySpeed(speedMode);
            agent.isStopped = false;
            
            if (debugMode) Debug.Log($"[MonsterMovement] GoTo called: {position}");
            
            return agent.SetDestination(position);
        }

        public void Chase(Transform target)
        {
            if (target == null) return;
            
            if (Vector3.SqrMagnitude(agent.destination - target.position) > 2.0f)
            {
                ResetTrackers();
            }

            agent.isStopped = false;
            ApplySpeed(SpeedState.Chase);
            agent.SetDestination(target.position);
        }

        public void Stop()
        {
            standStillTimer = 0f;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        public bool HasReachedDestination()
        {
            if (agent.pathPending) return false;
            if (!agent.hasPath) return false;

            // 1. Timeout Check
            if (standStillTimer > config.standStillTime)
            {
                if(debugMode) Debug.Log($"[MonsterMovement] ARRIVAL BY TIMEOUT ({standStillTimer:F1}s)");
                return true;
            }

            // 2. Distance Check
            if (agent.remainingDistance <= config.baseStoppingDistance)
            {
                return true;
            }

            return false;
        }

        private void ResetTrackers()
        {
            standStillTimer = 0f;
            movementStartTime = Time.time;
            positionAtLastCheck = transform.position;
            nextCheckTime = Time.time + 0.2f;
        }

        private void ApplySpeed(SpeedState mode)
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
                    agent.stoppingDistance = 0.5f; 
                    break;
                case SpeedState.Investigate:
                    agent.speed = config.investigateRushSpeed;
                    agent.acceleration = config.investigateRushAcceleration;
                    break;
            }
        }
    }
}