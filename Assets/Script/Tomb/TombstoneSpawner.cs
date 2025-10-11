// TombstoneSpawner.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class TombstoneSpawner : MonoBehaviour
{
    [Header("Tombstone")]
    public GameObject tombstonePrefab;
    public int maxSpawnCount = 20; // Will spawn up to this, but Poisson may yield fewer

    [Header("Spawn Area (X/Z Plane)")]
    public Vector3 areaCenter;
    public Vector2 areaSize = new Vector2(50f, 50f);

    [Header("Spacing")]
    public float minDistance = 6f; // Minimum distance between tombstones

    [Header("Ground & NavMesh")]
    public LayerMask groundLayer = 1; // Usually "Default" or "Ground"
    public bool useNavMesh = true;

    void Start()
    {
        SpawnTombstones();
    }

    void SpawnTombstones()
    {
        // Define 2D rect (X/Z)
        Rect spawnRect = new Rect(
            areaCenter.x - areaSize.x * 0.5f,
            areaCenter.z - areaSize.y * 0.5f,
            areaSize.x,
            areaSize.y
        );

        // Generate Poisson points (2D)
        Vector2[] poissonPoints = PoissonDiscSampling.GeneratePoints(minDistance, spawnRect);

        int spawned = 0;
        foreach (Vector2 point2D in poissonPoints)
        {
            if (spawned >= maxSpawnCount) break;

            Vector3 worldPos3D = new Vector3(point2D.x, areaCenter.y, point2D.y);
            Vector3? finalPosition = GetGroundPosition(worldPos3D);

            if (finalPosition.HasValue)
            {
                Instantiate(tombstonePrefab, finalPosition.Value, Quaternion.identity);
                spawned++;
            }
        }

        Debug.Log($"Spawned {spawned} tombstones using Poisson Disk Sampling.");
    }

    Vector3? GetGroundPosition(Vector3 position)
    {
        if (useNavMesh)
        {
            // Try NavMesh first
            if (NavMesh.SamplePosition(position, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
            {
                return navHit.position;
            }
        }

        // Fallback: Raycast to ground
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit rayHit, 20f, groundLayer))
        {
            return rayHit.point;
        }

        return null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(areaCenter, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}