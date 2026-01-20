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

    [Header("Height Adjustment")]
    [Tooltip("Random Y Offset added to the grounded position.")]
    public float minYOffset = 0f;
    public float maxYOffset = 0f;

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
    [Tooltip("How high from the center to start the raycast down.")]
    public float raycastHeight = 50f; 

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

        if (Application.isPlaying)
        {
            children.ForEach(child => Destroy(child));
        }
        else
        {
            children.ForEach(child => DestroyImmediate(child));
        }
    }

    void Start()
    {
        SpawnAllObjects();
    }

    void SpawnObjectsForPass(SpawnConfiguration config)
    {
        float desiredCount = config.maxObjectCount * config.density;
        if (desiredCount < 1) return;

        float totalArea = areaSize.x * areaSize.y;
        float areaPerObject = totalArea / desiredCount;
        
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
        int maxToSpawn = Mathf.RoundToInt(desiredCount);

        foreach (Vector2 point2D in candidatePoints)
        {
            if (spawnedInPass >= maxToSpawn) break;

            Vector3 worldPos = new Vector3(point2D.x, areaCenter.y, point2D.y);

            Vector3? finalPosition = GetValidGroundPosition(worldPos, config);

            if (finalPosition.HasValue)
            {
                if (IsPositionClear(finalPosition.Value, config.objectAvoidanceRadius, config))
                {
                    GameObject prefab = config.prefabs[Random.Range(0, config.prefabs.Length)];
                    
                    // 3. Apply Y Rotation Randomization
                    Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    GameObject spawnedObj = Instantiate(prefab, finalPosition.Value, rot);
                    spawnedObj.transform.SetParent(this.transform);
                    
                    spawnedObjects.Add((finalPosition.Value, config.objectAvoidanceRadius));
                    spawnedInPass++;
                }
            }
        }
    }
    Vector3? GetValidGroundPosition(Vector3 position, SpawnConfiguration config)
    {
        Vector3 rayStart = new Vector3(position.x, areaCenter.y + raycastHeight, position.z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, config.spawnOnLayer))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {

                Vector3 basePos = navHit.position;

                float randomOffset = Random.Range(config.minYOffset, config.maxYOffset);
                return basePos + Vector3.up * randomOffset;
            }
        }

        return null;
    }

    bool IsPositionClear(Vector3 position, float radius, SpawnConfiguration config)
    {
        if (Physics.CheckSphere(position, radius, config.avoidLayers)) return false;
        if (Physics.CheckSphere(position, 0.1f, config.noSpawnZoneLayer)) return false;
        
        foreach (var spawned in spawnedObjects)
        {
            float requiredDistance = spawned.radius + radius;
            float distSqr = (new Vector2(position.x, position.z) - new Vector2(spawned.position.x, spawned.position.z)).sqrMagnitude;
            if (distSqr < requiredDistance * requiredDistance) return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawCube(areaCenter, new Vector3(areaSize.x, 0.1f, areaSize.y));
        
        // Visualize Raycast Height
        Gizmos.color = Color.yellow;
        Vector3 corner = areaCenter - new Vector3(areaSize.x/2, 0, areaSize.y/2);
        Gizmos.DrawLine(corner + Vector3.up * raycastHeight, corner + Vector3.down * raycastHeight);
    }
}