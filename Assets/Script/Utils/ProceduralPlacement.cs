// ProceduralPlacement.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[System.Serializable]
public class SpawnConfiguration
{
    public string name;
    public GameObject[] prefabs;

    [Header("Density & Count")]
    [Tooltip("The maximum number of objects this pass will try to spawn.")]
    public int maxObjectCount = 100;
    [Tooltip("A percentage of the max count to actually spawn. 0 = none, 1 = max.")]
    [Range(0.01f, 1f)] public float density = 0.5f;

    [Header("Spacing & Avoidance")]
    [Tooltip("How much personal space each object requires.")]
    public float objectAvoidanceRadius = 2f;

    [Header("Placement Rules")]
    public LayerMask spawnOnLayer = 1;
    public LayerMask avoidLayers;
    public LayerMask noSpawnZoneLayer;
}

public class ProceduralPlacement : MonoBehaviour
{
    [Header("Spawn Area (X/Z Plane)")]
    public Vector3 areaCenter;
    public Vector2 areaSize = new Vector2(100f, 100f);

    [Header("Spawning Passes (Processed in Order)")]
    public List<SpawnConfiguration> spawnPasses = new List<SpawnConfiguration>();
    
    private List<(Vector3 position, float radius)> spawnedObjects = new List<(Vector3, float)>();

    [ContextMenu("Spawn All Objects")]
    void SpawnAllObjects()
    {
        ClearAllObjects(); 
        spawnedObjects.Clear();
        foreach (var config in spawnPasses)
        {
            SpawnObjectsForPass(config);
        }
        Debug.Log($"Procedural placement complete. Spawned a total of {spawnedObjects.Count} objects.");
    }
    
    [ContextMenu("Clear All Objects")]
    void ClearAllObjects()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        // Use different destruction methods for Editor vs. Play Mode
        if (Application.isPlaying)
        {
            // At runtime, use the standard Destroy()
            children.ForEach(child => Destroy(child));
        }
        else
        {
            // In the editor, use DestroyImmediate()
            children.ForEach(child => DestroyImmediate(child));
        }
    }

    void Start()
    {
        SpawnAllObjects();
    }
    void SpawnObjectsForPass(SpawnConfiguration config)
    {
        // --- KEY CHANGE: Calculate Poisson Radius from Density and Count ---
        float desiredCount = config.maxObjectCount * config.density;
        if (desiredCount < 1) return;

        float totalArea = areaSize.x * areaSize.y;
        float areaPerObject = totalArea / desiredCount;
        
        // The Poisson radius is roughly the sqrt of the area per object.
        // We clamp it to the object's avoidance radius to prevent impossible densities.
        float poissonRadius = Mathf.Sqrt(areaPerObject);
        poissonRadius = Mathf.Max(poissonRadius, config.objectAvoidanceRadius);
        
        Rect spawnRect = new Rect(
            areaCenter.x - areaSize.x * 0.5f,
            areaCenter.z - areaSize.y * 0.5f,
            areaSize.x,
            areaSize.y
        );

        Vector2[] candidatePoints = PoissonDiscSampling.GeneratePoints(poissonRadius, spawnRect);
        int spawnedInPass = 0;
        
        // --- KEY CHANGE: Cap the number of spawned objects ---
        int maxToSpawn = Mathf.RoundToInt(desiredCount);

        foreach (Vector2 point2D in candidatePoints)
        {
            if (spawnedInPass >= maxToSpawn) break; // Stop when we reach our desired count

            Vector3 worldPos = new Vector3(point2D.x, areaCenter.y, point2D.y);
            Vector3? finalPosition = GetValidGroundPosition(worldPos);

            if (finalPosition.HasValue)
            {
                if (IsPositionClear(finalPosition.Value, config.objectAvoidanceRadius, config))
                {
                    GameObject prefab = config.prefabs[Random.Range(0, config.prefabs.Length)];
                    GameObject spawnedObj = Instantiate(prefab, finalPosition.Value, Quaternion.Euler(0, Random.Range(0, 360), 0));
                    spawnedObj.transform.SetParent(this.transform);
                    spawnedObjects.Add((finalPosition.Value, config.objectAvoidanceRadius));
                    spawnedInPass++;
                }
            }
        }
        Debug.Log($"Pass '{config.name}': Spawned {spawnedInPass} objects (target was ~{maxToSpawn}).");
    }

    // ... (The rest of the script: IsPositionClear, GetValidGroundPosition, OnDrawGizmosSelected) remains exactly the same.
    bool IsPositionClear(Vector3 position, float radius, SpawnConfiguration config)
    {
        if (Physics.CheckSphere(position, radius, config.avoidLayers)) return false;
        if (Physics.CheckSphere(position, 0.1f, config.noSpawnZoneLayer)) return false;
        foreach (var spawned in spawnedObjects)
        {
            float requiredDistance = spawned.radius + radius;
            if (Vector3.Distance(position, spawned.position) < requiredDistance) return false;
        }
        return true;
    }
    Vector3? GetValidGroundPosition(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit navHit, 20f, NavMesh.AllAreas)) return navHit.position;
        return null;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawCube(areaCenter, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}