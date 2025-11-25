using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class CoverFinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonsterConfig config;

        // Cache list to avoid GC
        private readonly List<Vector3> _foundPoints = new List<Vector3>();

        private void Awake()
        {
            if (config == null) config = GetComponent<MonsterConfig>();
        }

        // We keep the signature accepting monsterPos to sort by distance
        public List<Vector3> GetCoverPointsAround(Vector3 centerOfSearch, Vector3 monsterPos)
        {
            _foundPoints.Clear();

            float radius = config.investigateRadius;
            LayerMask obstacleMask = config.obstacleLayerMask;
            int rayCount = 12; // 12 checks in a circle
            
            // TWEAK: Increased for Large Monster (Scale 7).
            // If this is too small, the monster touches the wall before reaching the point.
            float behindOffset = 5.0f; 
            
            float angleStep = 360f / rayCount;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                // 1. SphereCast (Find Trees/Walls)
                if (Physics.SphereCast(centerOfSearch, 0.5f, dir, out RaycastHit hit, radius, obstacleMask))
                {
                    // 2. Calculate point BEHIND the obstacle
                    Vector3 toObstacle = (hit.point - centerOfSearch).normalized;
                    Vector3 hidingSpot = hit.point + toObstacle * behindOffset;

                    // 3. Snap to NavMesh
                    // Used 5.0f radius to ensure we catch the floor even on uneven terrain
                    if (NavMesh.SamplePosition(hidingSpot, out NavMeshHit navHit, 5.0f, NavMesh.AllAreas))
                    {
                        // 4. Basic duplicate check (don't add points too close to each other)
                        bool closeToExisting = false;
                        foreach(var p in _foundPoints)
                        {
                            if(Vector3.Distance(p, navHit.position) < 2.0f) 
                            { 
                                closeToExisting = true; 
                                break; 
                            }
                        }

                        if (!closeToExisting)
                        {
                            _foundPoints.Add(navHit.position);
                        }
                    }
                }
            }
            
            // Sort by distance (Visit closest cover first)
            _foundPoints.Sort((a, b) => Vector3.Distance(monsterPos, a).CompareTo(Vector3.Distance(monsterPos, b)));

            // Limit count based on config
            if (_foundPoints.Count > config.investigationPoints)
            {
                return _foundPoints.GetRange(0, config.investigationPoints);
            }

            return new List<Vector3>(_foundPoints);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var p in _foundPoints)
            {
                Gizmos.DrawSphere(p, 0.5f);
            }
        }
    }
}