// SpawnArea.cs
using UnityEngine;

// Require a collider so we always have a shape to define the area
[RequireComponent(typeof(Collider))]
public class SpawnArea : MonoBehaviour
{
    [Tooltip("The profile defining WHAT should be spawned in this area.")]
    public SpawnProfile profile;
    private Collider areaCollider;

    void Awake()
    {
        areaCollider = GetComponent<Collider>();
        // Make sure the collider is a trigger so it doesn't physically block anything
        areaCollider.isTrigger = true; 
    }

    // This method will be called by the manager to start the spawning process
    public void ExecuteSpawning()
    {
        if (profile == null)
        {
            Debug.LogWarning($"Spawn Area '{gameObject.name}' has no profile assigned.", this);
            return;
        }

        Bounds bounds = areaCollider.bounds;
        Rect spawnRect = new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z);
        
        Vector2[] points = PoissonDiscSampling.GeneratePoints(profile.minDistance, spawnRect);

        int spawnedCount = 0;
        foreach (var point in points)
        {
            if (spawnedCount >= profile.maxSpawnCount) break;
            
            // Initial position is in the collider's bounds, but not necessarily its SHAPE
            Vector3 candidatePoint = new Vector3(point.x, bounds.center.y, point.y);
            
            // IMPORTANT: Check if the point is within the actual collider shape, not just its bounding box.
            if (areaCollider.ClosestPoint(candidatePoint) != candidatePoint)
            {
                continue; // This point is in the bounding box, but outside the shape (e.g., for spheres/meshes)
            }
            
            // Now, validate and place the object
            TryPlaceObject(candidatePoint);
            spawnedCount++;
        }
    }
    
    private void TryPlaceObject(Vector3 origin)
    {
        // 1. Find Ground: Raycast down to find the terrain surface
        if (!Physics.Raycast(origin + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, profile.spawnableLayers))
        {
            return; // Couldn't find valid ground below this point
        }
        
        Vector3 finalPosition = hit.point;
        
        // 2. Check for Obstacles: Use CheckSphere to see if we're too close to anything
        if (Physics.CheckSphere(finalPosition, profile.clearanceRadius, profile.obstacleLayers))
        {
            return; // Too close to a tombstone, building, or other obstacle
        }

        // --- All checks passed! Let's spawn. ---

        // 3. Select a random prefab
        GameObject prefabToSpawn = profile.spawnablePrefabs[Random.Range(0, profile.spawnablePrefabs.Count)];
        
        // 4. Determine rotation
        Quaternion finalRotation = Quaternion.identity;
        if (profile.alignToGroundNormal)
        {
            // Rotate to match the slope of the ground
            finalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal); 
        }
        else
        {
            // Give it a random rotation around the Y axis
            finalRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        }

        // 5. Instantiate and customize
        GameObject spawnedObject = Instantiate(prefabToSpawn, finalPosition, finalRotation, this.transform); // Spawn as a child
        spawnedObject.transform.localScale = Vector3.one * Random.Range(profile.scaleRange.x, profile.scaleRange.y);
    }
}