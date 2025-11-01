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
    }
}