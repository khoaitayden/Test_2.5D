using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class CoverFinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonsterConfig config;

        // Optimization: Recycle this list to avoid creating Garbage (GC) every search
        private readonly List<Vector3> _foundPoints = new List<Vector3>();

        private void Awake()
        {
            if (config == null) config = GetComponent<MonsterConfig>();
        }

        public List<Vector3> GetCoverPointsAround(Vector3 centerOfSearch)
        {
            _foundPoints.Clear();

            // Settings derived from Config
            float radius = config.investigateRadius;
            LayerMask obstacleMask = config.obstacleLayerMask;
            int rayCount = 12;
            float behindOffset = 2.0f; // How far behind the wall to check
            
            float angleStep = 360f / rayCount;

            for (int i = 0; i < rayCount; i++)
            {
                // 1. Math - Direction calculation
                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                // 2. Physics - Find obstacles around the player/center
                // SphereCast gives us some thickness so we don't hit tiny wires
                if (Physics.SphereCast(centerOfSearch, 0.5f, dir, out RaycastHit hit, radius, obstacleMask))
                {
                    // Calculate a spot BEHIND that obstacle
                    Vector3 toObstacle = (hit.point - centerOfSearch).normalized;
                    
                    // MODIFIED: 'behindOffset' puts it behind the wall. 
                    // We must ensure this offset doesn't push it into another wall.
                    Vector3 hidingSpot = hit.point + toObstacle * behindOffset; 

                    if (NavMesh.SamplePosition(hidingSpot, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                    {
                        // NEW: Verify the snapped point is not super close to the original hit (which was the wall)
                        // This prevents points that are literally hugging the collider
                        _foundPoints.Add(navHit.position);
                    }
                }
            }
            
            // Limit points based on config here so the Action doesn't have to logic-check
            // Optional: Shuffle the list here if you want randomness

            // Return a copy so the list doesn't get modified externally unexpectedly
            return new List<Vector3>(_foundPoints);
        }

        private bool CheckVisibility(Vector3 eyePos, Vector3 targetPos, LayerMask layerMask)
        {
            // Simple Raycast: Start a bit up to avoid floor friction
            Vector3 start = eyePos + Vector3.up * 1f;
            Vector3 end = targetPos + Vector3.up * 1f;
            Vector3 dir = end - start;
            
            // If the ray hits something, we are hidden (Success)
            // If it hits nothing, we can be seen (Failure)
            return Physics.Raycast(start, dir.normalized, dir.magnitude, layerMask);
        }
        
        // Debug helper
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            foreach (var p in _foundPoints)
            {
                Gizmos.DrawSphere(p, 0.3f);
            }
        }
    }
}