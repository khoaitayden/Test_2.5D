using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class CoverFinder
{
    public static List<Vector3> FindCoverPoints(Vector3 searchCenter, float searchRadius, Vector3 monsterPosition, MonsterConfig config)
    {
        var coverPoints = new List<Vector3>();
        int numberOfCasts = 16; // Number of rays to cast in a circle
        float behindCoverDistance = 5f; // How far behind the obstacle to place the point
        float minPointDistance = 20f; // Minimum distance between cover points
        float debugDuration = 100f;

        for (int i = 0; i < numberOfCasts; i++)
        {
            float angle = i * (360f / numberOfCasts);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            // Cast from search center outward to find obstacles
            if (Physics.SphereCast(searchCenter, 0.5f, direction, out RaycastHit hit, searchRadius, config.obstacleLayerMask))
            {
                // Draw RED ray to the obstacle
                Debug.DrawLine(searchCenter, hit.point, Color.red, debugDuration);
                
                // Calculate point BEHIND the cover (opposite side from search center)
                Vector3 toObstacle = (hit.point - searchCenter).normalized;
                Vector3 behindCoverPoint = hit.point + toObstacle * behindCoverDistance;
                
                // Draw YELLOW ray showing the "behind cover" direction
                Debug.DrawLine(hit.point, behindCoverPoint, Color.yellow, debugDuration);

                // Verify the point is on the NavMesh
                if (NavMesh.SamplePosition(behindCoverPoint, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                {
                    Vector3 validPoint = navHit.position;
                    
                    // Ensure monster can path to this location
                    NavMeshPath path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(monsterPosition, validPoint, NavMesh.AllAreas, path) || 
                        path.status != NavMeshPathStatus.PathComplete)
                    {
                        // Draw MAGENTA ray for unreachable points
                        Debug.DrawRay(validPoint, Vector3.up * 2f, Color.magenta, debugDuration);
                        continue;
                    }

                    // Check if point is too close to existing cover points
                    bool isTooClose = false;
                    foreach (var point in coverPoints)
                    {
                        if (Vector3.Distance(validPoint, point) < minPointDistance)
                        {
                            isTooClose = true;
                            break;
                        }
                    }

                    if (!isTooClose)
                    {
                        // Verify this point actually provides cover from search center
                        if (IsPointBehindCover(validPoint, searchCenter, config.obstacleLayerMask))
                        {
                            coverPoints.Add(validPoint);
                            // Draw GREEN ray for valid, accepted cover points
                            Debug.DrawRay(validPoint, Vector3.up * 3f, Color.green, debugDuration);
                        }
                        else
                        {
                            // Draw CYAN ray for points that don't actually provide cover
                            Debug.DrawRay(validPoint, Vector3.up * 2f, Color.cyan, debugDuration);
                        }
                    }
                }
                else
                {
                    // Draw GREY line for points not on NavMesh
                    Debug.DrawLine(hit.point, behindCoverPoint, Color.grey, debugDuration);
                }
            }
            else
            {
                // Draw WHITE ray for directions with no obstacles
                Debug.DrawRay(searchCenter, direction * searchRadius, Color.white, debugDuration);
            }
        }
        
        Debug.Log($"[CoverFinder] Found {coverPoints.Count} cover points behind obstacles.");
        return coverPoints;
    }

    /// <summary>
    /// Verifies that the cover point is actually hidden from the search center by an obstacle.
    /// </summary>
    private static bool IsPointBehindCover(Vector3 coverPoint, Vector3 searchCenter, LayerMask obstacleLayer)
    {
        // Cast from search center toward the cover point
        Vector3 directionToPoint = (coverPoint - searchCenter).normalized;
        float distanceToPoint = Vector3.Distance(searchCenter, coverPoint);
        
        // Raise the ray slightly to avoid ground collision
        Vector3 rayStart = searchCenter + Vector3.up * 0.5f;
        Vector3 rayTarget = coverPoint + Vector3.up * 0.5f;
        Vector3 rayDirection = (rayTarget - rayStart).normalized;
        float rayDistance = Vector3.Distance(rayStart, rayTarget);
        
        // If there's an obstacle between search center and cover point, it's valid cover
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            // Draw BLUE debug ray showing the cover verification
            Debug.DrawLine(rayStart, hit.point, Color.blue, 5f);
            return true;
        }
        
        // No obstacle found = not valid cover
        return false;
    }
}