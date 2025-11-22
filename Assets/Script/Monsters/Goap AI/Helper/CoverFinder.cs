using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class CoverFinder
{
    // Reuse memory list to prevent Garbage Collection spikes
    private static List<Vector3> _cachedCoverPoints = new List<Vector3>();

    public static List<Vector3> FindCoverPoints(Vector3 searchCenter, float searchRadius, Vector3 monsterPosition, MonsterConfig config)
    {
        _cachedCoverPoints.Clear();
        
        int numberOfCasts = 12; // Reduced from 16 (16 is overkill for radius < 20)
        float behindCoverDistance = 3f; 
        float minPointDistance = 5f; // Keep points somewhat spread
        
        // Pre-calculate rotation to avoid Quaternion math inside loop if possible, but Euler is okay here
        float angleStep = 360f / numberOfCasts;

        for (int i = 0; i < numberOfCasts; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            // 1. SphereCast (Physics) - Cheap enough
            if (Physics.SphereCast(searchCenter, 0.5f, direction, out RaycastHit hit, searchRadius, config.obstacleLayerMask))
            {
                Vector3 toObstacle = (hit.point - searchCenter).normalized;
                Vector3 behindCoverPoint = hit.point + toObstacle * behindCoverDistance;

                // 2. NavMesh.SamplePosition (Geometry) - Fast
                // We check if there is "ground" at that point.
                // CRITICAL OPTIMIZATION: We removed CalculatePath here.
                if (NavMesh.SamplePosition(behindCoverPoint, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                {
                    // 3. Verification (Logic) - Very Fast
                    bool isTooClose = false;
                    for (int j = 0; j < _cachedCoverPoints.Count; j++)
                    {
                        if ((_cachedCoverPoints[j] - navHit.position).sqrMagnitude < minPointDistance * minPointDistance)
                        {
                            isTooClose = true;
                            break;
                        }
                    }

                    if (!isTooClose)
                    {
                        // Simple line check to ensure cover validity
                        if (IsPointBehindCover(navHit.position, searchCenter, config.obstacleLayerMask))
                        {
                            _cachedCoverPoints.Add(navHit.position);
                            // Debug visualizers kept as requested
                            Debug.DrawRay(navHit.position, Vector3.up * 2f, Color.green, 2f); 
                        }
                    }
                }
            }
        }
        
        // Return a new list copy so we don't have reference issues if multiple monsters call this same frame
        return new List<Vector3>(_cachedCoverPoints);
    }

    private static bool IsPointBehindCover(Vector3 coverPoint, Vector3 searchCenter, LayerMask obstacleLayer)
    {
        Vector3 start = searchCenter + Vector3.up * 0.5f;
        Vector3 target = coverPoint + Vector3.up * 0.5f;
        Vector3 dir = target - start;
        
        return Physics.Raycast(start, dir.normalized, dir.magnitude, obstacleLayer);
    }
}