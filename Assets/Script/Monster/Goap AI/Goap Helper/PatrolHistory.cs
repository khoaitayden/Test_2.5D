using System.Collections.Generic;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    /// <summary>
    /// Tracks patrol history to avoid revisiting recent locations
    /// </summary>
    public class PatrolHistory : MonoBehaviour
    {
        private Queue<Vector3> recentPatrolPoints = new Queue<Vector3>();
        private int maxHistorySize = 5; // Remember last 5 patrol points

        public void RecordPatrolPoint(Vector3 position)
        {
            recentPatrolPoints.Enqueue(position);
            
            // Remove oldest if we exceed max size
            while (recentPatrolPoints.Count > maxHistorySize)
            {
                recentPatrolPoints.Dequeue();
            }
            
            Debug.Log($"[PatrolHistory] Recorded point. History size: {recentPatrolPoints.Count}");
        }

        public bool IsTooCloseToRecentPoints(Vector3 candidatePosition, float minDistance)
        {
            foreach (Vector3 recentPoint in recentPatrolPoints)
            {
                float distance = Vector3.Distance(candidatePosition, recentPoint);
                if (distance < minDistance)
                {
                    return true; // Too close to a recent point
                }
            }
            return false;
        }

        public void Clear()
        {
            recentPatrolPoints.Clear();
            Debug.Log("[PatrolHistory] History cleared.");
        }

        public int GetHistoryCount()
        {
            return recentPatrolPoints.Count;
        }

        public void SetMaxHistorySize(int size)
        {
            maxHistorySize = Mathf.Max(1, size);
        }
    }
}