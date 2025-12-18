using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class CoverFinder : MonoBehaviour
    {
        [SerializeField] private MonsterConfig config;
        private Queue<Vector3> searchQueue = new Queue<Vector3>();

        public bool HasPoints => searchQueue.Count > 0;

        private void Awake()
        {
            if (config == null) config = GetComponent<MonsterConfig>();
        }

        public Vector3 GetCurrentPoint()
        {
            return searchQueue.Count > 0 ? searchQueue.Peek() : transform.position;
        }

        public void AdvanceQueue()
        {
            if (searchQueue.Count > 0) searchQueue.Dequeue();
        }

        public void GeneratePoints(Vector3 center, Vector3 monsterPos)
        {
            searchQueue.Clear();
            List<Vector3> candidates = new List<Vector3>();
            
            float radius = config.investigateRadius;
            int rayCount = config.numCoverFinderRayCasts; // Use Config Variable

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0, i * (360f / rayCount), 0) * Vector3.forward;
                
                // Simple Raycast
                if (Physics.Raycast(center + Vector3.up, dir, out RaycastHit hit, radius, config.obstacleLayerMask))
                {
                    // 3.0f behind wall
                    Vector3 hidingSpot = hit.point + dir * 3.0f; 

                    // Is it on NavMesh?
                    if (NavMesh.SamplePosition(hidingSpot, out NavMeshHit navHit, 5.0f, NavMesh.AllAreas))
                    {
                        Vector3 validPoint = navHit.position;

                        // --- EDGE SAFETY CHECK (NEW FIX) ---
                        // If the point is exactly on the edge of the mesh (near building), nudge it.
                        if (NavMesh.FindClosestEdge(validPoint, out NavMeshHit edgeHit, NavMesh.AllAreas))
                        {
                            if (edgeHit.distance < 1.0f) // Too close to edge
                            {
                                // Move 1.5m away from the edge normal
                                validPoint = edgeHit.position + edgeHit.normal * 1.5f;
                            }
                        }

                        // --- DUPLICATE CHECK ---
                        bool isTooClose = false;
                        foreach (Vector3 existingPoint in candidates)
                        {
                            if (Vector3.Distance(validPoint, existingPoint) < config.minCoverPointDistance)
                            {
                                isTooClose = true;
                                break;
                            }
                        }

                        if (!isTooClose)
                        {
                            candidates.Add(validPoint);
                        }
                    }
                }
            }

            // Sort by distance
            candidates.Sort((a, b) => Vector3.Distance(monsterPos, a).CompareTo(Vector3.Distance(monsterPos, b)));

            // Fill Queue
            int count = Mathf.Min(candidates.Count, config.investigationPoints);
            for (int i = 0; i < count; i++) searchQueue.Enqueue(candidates[i]);
            
            Debug.Log($"[CoverFinder] Generated {searchQueue.Count} points.");
        }
        
        public void Clear() => searchQueue.Clear();
    }
}