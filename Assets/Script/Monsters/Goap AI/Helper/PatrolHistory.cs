using System.Collections.Generic;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PatrolHistory : MonoBehaviour
    {
        private Queue<Vector3> recentPatrolPoints = new Queue<Vector3>();
        private MonsterConfig config;

        private void Awake()
        {
            config = GetComponent<MonsterConfig>();
        }

        public void RecordPatrolPoint(Vector3 position)
        {
            int maxSize = (config != null) ? config.patrolHistorySize : 5;

            recentPatrolPoints.Enqueue(position);
            
            // Keep only the last N points
            while (recentPatrolPoints.Count > maxSize)
            {
                recentPatrolPoints.Dequeue();
            }
            
            // Debug.Log($"[PatrolHistory] Recorded point. History size: {recentPatrolPoints.Count}/{maxSize}");
        }

        public bool IsTooCloseToRecentPoints(Vector3 candidatePosition, float minDistance)
        {
            foreach (Vector3 recentPoint in recentPatrolPoints)
            {
                if (Vector3.Distance(candidatePosition, recentPoint) < minDistance)
                {
                    return true; 
                }
            }
            return false;
        }
    }
}