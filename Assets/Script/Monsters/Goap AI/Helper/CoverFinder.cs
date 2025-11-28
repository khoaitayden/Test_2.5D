using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class CoverFinder : MonoBehaviour
    {
        [SerializeField] private MonsterConfig config;
        
        // The Queue of points to visit
        private Queue<Vector3> searchQueue = new Queue<Vector3>();
        private NavMeshPath testPath;

        public bool HasPoints => searchQueue.Count > 0;

        private void Awake()
        {
            if (config == null) config = GetComponent<MonsterConfig>();
            testPath = new NavMeshPath();
        }

        public Vector3 GetCurrentPoint()
        {
            if (searchQueue.Count > 0) return searchQueue.Peek();
            return transform.position;
        }

        // Called by the Action when it finishes checking a spot
        public void AdvanceQueue()
        {
            if (searchQueue.Count > 0)
            {
                searchQueue.Dequeue();
            }
        }

        public void GeneratePoints(Vector3 center, Vector3 monsterPos)
        {
            searchQueue.Clear();
            List<Vector3> candidates = new List<Vector3>();

            float radius = config.investigateRadius;
            int rayCount = 12;
            
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0, i * (360f / rayCount), 0) * Vector3.forward;

                if (Physics.Raycast(center + Vector3.up, dir, out RaycastHit hit, radius, config.obstacleLayerMask))
                {
                    // 4.0f behind wall to give space
                    Vector3 hidingSpot = hit.point + dir * 4.0f;

                    if (NavMesh.SamplePosition(hidingSpot, out NavMeshHit navHit, 10.0f, NavMesh.AllAreas))
                    {
                        // Ensure point is reachable
                        if (NavMesh.CalculatePath(monsterPos, navHit.position, NavMesh.AllAreas, testPath) 
                            && testPath.status == NavMeshPathStatus.PathComplete)
                        {
                            candidates.Add(navHit.position);
                        }
                    }
                }
            }

            // Sort by distance (Visit closest first)
            candidates.Sort((a, b) => Vector3.Distance(monsterPos, a).CompareTo(Vector3.Distance(monsterPos, b)));

            // Fill Queue (Limit to config count)
            int count = Mathf.Min(candidates.Count, config.investigationPoints);
            for (int i = 0; i < count; i++)
            {
                searchQueue.Enqueue(candidates[i]);
            }
            
            Debug.Log($"[CoverFinder] Generated {searchQueue.Count} points.");
        }
        
        // Helper to clear if investigation is totally cancelled
        public void Clear() => searchQueue.Clear();
    }
}