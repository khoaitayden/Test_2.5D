using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        private MonsterConfig config;
        private PatrolHistory patrolHistory;
        private const int MaxAttempts = 50; // Increased because we have more constraints

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            // Cache components
            if (config == null)
            {
                config = references.GetCachedComponent<MonsterConfig>();
                if (config == null)
                {
                    Debug.LogError("[PatrolSensor] Requires MonsterConfig component!");
                    return null;
                }
            }

            if (patrolHistory == null)
            {
                patrolHistory = references.GetCachedComponent<PatrolHistory>();
                if (patrolHistory == null)
                {
                    // Add the component if it doesn't exist
                    patrolHistory = agent.Transform.gameObject.AddComponent<PatrolHistory>();
                    patrolHistory.SetMaxHistorySize(config.patrolHistorySize);
                }
            }

            Vector3? validPosition = GetSmartPatrolPosition(agent);

            if (!validPosition.HasValue)
            {
                Debug.LogWarning("[PatrolSensor] Could not find valid patrol point! Keeping existing target.");
                return existingTarget;
            }

            Vector3 targetPosition = validPosition.Value;

            // Record this point in history
            patrolHistory.RecordPatrolPoint(targetPosition);

            if (existingTarget is PositionTarget positionTarget)
            {
                return positionTarget.SetPosition(targetPosition);
            }

            return new PositionTarget(targetPosition);
        }

        /// <summary>
        /// Finds a smart patrol position that:
        /// 1. Is reachable via NavMesh
        /// 2. Isn't too close to recent patrol points
        /// 3. Prefers forward direction (configurable)
        /// </summary>
        private Vector3? GetSmartPatrolPosition(IActionReceiver agent)
        {
            Vector3 origin = agent.Transform.position;
            Vector3 forward = agent.Transform.forward;

            for (int i = 0; i < MaxAttempts; i++)
            {
                Vector3 candidatePoint;

                // Use forward bias to prefer continuing in current direction
                if (Random.value < config.forwardBias)
                {
                    // Forward-biased direction (within a cone ahead)
                    float angle = Random.Range(-60f, 60f); // 120° cone ahead
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
                    float distance = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                    candidatePoint = origin + direction.normalized * distance;
                }
                else
                {
                    // Random direction (for variety)
                    Vector2 randomDirection = Random.insideUnitCircle.normalized;
                    float randomDistance = Random.Range(config.minPatrolDistance, config.maxPatrolDistance);
                    candidatePoint = origin + new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
                }

                // Find nearest point on NavMesh
                if (!NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, config.maxPatrolDistance, NavMesh.AllAreas))
                    continue;

                // RULE 1: Check if reachable
                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path))
                    continue;
                
                if (path.status != NavMeshPathStatus.PathComplete)
                    continue;

                // RULE 2: Check if too close to recent patrol points
                if (patrolHistory.IsTooCloseToRecentPoints(hit.position, config.minDistanceFromRecentPoints))
                {
                    continue; // Skip this point, try another
                }

                // RULE 3: Prefer points that are a good distance away (not too close)
                float distanceFromOrigin = Vector3.Distance(origin, hit.position);
                if (distanceFromOrigin < config.minPatrolDistance * 0.8f)
                    continue;

                // Found a valid point!
                Debug.Log($"[PatrolSensor] Found valid patrol point at distance {distanceFromOrigin:F1}m " +
                         $"(attempt {i + 1}/{MaxAttempts})");
                return hit.position;
            }

            Debug.LogWarning($"[PatrolSensor] Could not find valid patrol point after {MaxAttempts} attempts!");
            return null;
        }
    }
}