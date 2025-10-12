// SpawnProfile.cs
using UnityEngine;
using System.Collections.Generic;

// This allows you to create instances of this profile in the Project window
[CreateAssetMenu(fileName = "New Spawn Profile", menuName = "Procedural/Spawn Profile")]
public class SpawnProfile : ScriptableObject
{
    [Header("Spawning Rules")]
    [Tooltip("The list of prefabs to choose from for spawning.")]
    public List<GameObject> spawnablePrefabs;

    [Tooltip("Minimum distance between each spawned object.")]
    public float minDistance = 5f;

    [Tooltip("Maximum number of objects to spawn in an area. The final count may be less due to validation checks.")]
    public int maxSpawnCount = 100;
    
    [Header("Placement Validation")]
    [Tooltip("The layers that these objects are allowed to spawn on (e.g., 'Ground').")]
    public LayerMask spawnableLayers = 1;

    [Tooltip("The layers that these objects must AVOID (e.g., 'Tombstone', 'Building', 'Water').")]
    public LayerMask obstacleLayers;

    [Tooltip("How much empty space to require around an object after it's placed.")]
    public float clearanceRadius = 2f;
    
    [Header("Randomization")]
    [Tooltip("The minimum (x) and maximum (y) scale the object can be.")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    [Tooltip("Should the spawned object align to the slope of the ground?")]
    public bool alignToGroundNormal = true;
}