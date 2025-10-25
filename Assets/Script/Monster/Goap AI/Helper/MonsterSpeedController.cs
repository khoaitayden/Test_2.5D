using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    /// <summary>
    /// Handles smooth speed transitions for the monster's NavMeshAgent
    /// </summary>
    public static class MonsterSpeedController
    {
        public enum SpeedMode
        {
            Patrol,
            Chase,
            InvestigateRush,
            InvestigateSearch
        }

        /// <summary>
        /// Set NavMeshAgent to the specified speed mode with proper acceleration
        /// </summary>
        public static void SetSpeedMode(NavMeshAgent agent, MonsterConfig config, SpeedMode mode)
        {
            if (agent == null || config == null) return;

            switch (mode)
            {
                case SpeedMode.Patrol:
                    agent.speed = config.patrolSpeed;
                    agent.acceleration = config.patrolAcceleration;
                    Debug.Log($"[SpeedController] Mode: PATROL (speed: {config.patrolSpeed}, accel: {config.patrolAcceleration})");
                    break;

                case SpeedMode.Chase:
                    agent.speed = config.chaseSpeed;
                    agent.acceleration = config.chaseAcceleration;
                    Debug.Log($"[SpeedController] Mode: CHASE (speed: {config.chaseSpeed}, accel: {config.chaseAcceleration})");
                    break;

                case SpeedMode.InvestigateRush:
                    agent.speed = config.investigateRushSpeed;
                    agent.acceleration = config.investigateRushAcceleration;
                    Debug.Log($"[SpeedController] Mode: INVESTIGATE RUSH (speed: {config.investigateRushSpeed}, accel: {config.investigateRushAcceleration})");
                    break;

                case SpeedMode.InvestigateSearch:
                    agent.speed = config.investigateSearchSpeed;
                    agent.acceleration = config.investigateSearchAcceleration;
                    Debug.Log($"[SpeedController] Mode: INVESTIGATE SEARCH (speed: {config.investigateSearchSpeed}, accel: {config.investigateSearchAcceleration})");
                    break;
            }
        }

        /// <summary>
        /// Gradually decrease speed during investigation (progressive slowdown)
        /// </summary>
        public static void UpdateInvestigationSpeed(NavMeshAgent agent, MonsterConfig config, float investigationProgress)
        {
            if (agent == null || config == null) return;

            // Progress from 0 to 1 (0 = just started, 1 = about to give up)
            float normalizedProgress = Mathf.Clamp01(investigationProgress / config.maxInvestigationTime);

            // Lerp from search speed to minimum speed
            float currentSpeed = Mathf.Lerp(
                config.investigateSearchSpeed,
                config.investigateMinSpeed,
                normalizedProgress
            );

            agent.speed = currentSpeed;
            
            // Also slightly reduce acceleration as we slow down (more cautious)
            float currentAccel = Mathf.Lerp(
                config.investigateSearchAcceleration,
                config.patrolAcceleration,
                normalizedProgress * 0.5f // Only reduce accel by 50% of the speed reduction
            );
            
            agent.acceleration = currentAccel;
        }
    }
}