using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    /// <summary>
    /// Reusable stuck detection helper that can be used by any action
    /// </summary>
    public class StuckDetector
    {
        private Vector3 anchorPosition;
        private float timeAtPosition;
        private bool isTracking;

        public void StartTracking(Vector3 startPosition)
        {
            anchorPosition = startPosition;
            timeAtPosition = 0f;
            isTracking = true;
        }

        public void Reset()
        {
            isTracking = false;
            timeAtPosition = 0f;
        }

        /// <summary>
        /// Check if the agent is stuck
        /// </summary>
        /// <returns>True if stuck for too long, false otherwise</returns>
        public bool CheckStuck(Vector3 currentPosition, float deltaTime, MonsterConfig config)
        {
            if (!isTracking)
                return false;

            float distanceFromAnchor = Vector3.Distance(currentPosition, anchorPosition);

            if (distanceFromAnchor <= config.stuckDistanceThreshold)
            {
                // Still near the anchor point, increment timer
                timeAtPosition += deltaTime;

                if (timeAtPosition >= config.maxStuckTime)
                {
                    Debug.LogWarning($"[StuckDetector] STUCK! Stayed within {config.stuckDistanceThreshold}m for {timeAtPosition:F2}s");
                    return true;
                }
            }
            else
            {
                // Moved far enough, reset the anchor
                anchorPosition = currentPosition;
                timeAtPosition = 0f;
            }

            return false;
        }

        public float GetTimeAtCurrentPosition()
        {
            return timeAtPosition;
        }
    }
}