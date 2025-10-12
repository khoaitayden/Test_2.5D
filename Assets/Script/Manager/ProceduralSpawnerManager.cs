// ProceduralSpawnerManager.cs
using UnityEngine;

public class ProceduralSpawnerManager : MonoBehaviour
{
    [Tooltip("If true, all Spawn Areas will be triggered when the game starts.")]
    public bool spawnOnStart = true;
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllAreas();
        }
    }
    
    // Allows you to trigger spawning from a button in the Inspector
    [ContextMenu("Spawn All Areas")]
    public void SpawnAllAreas()
    {
        Debug.Log("--- Starting Procedural Spawning ---");
        SpawnArea[] areas = FindObjectsOfType<SpawnArea>();
        foreach (SpawnArea area in areas)
        {
            area.ExecuteSpawning();
        }
        Debug.Log("--- Procedural Spawning Complete ---");
    }

    [ContextMenu("Destroy All Spawned Children")]
    public void ClearAllAreas()
    {
        Debug.Log("--- Clearing Procedurally Spawned Objects ---");
        SpawnArea[] areas = FindObjectsOfType<SpawnArea>();
        foreach (SpawnArea area in areas)
        {
            // Destroy all children of the spawn area in reverse order
            for (int i = area.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(area.transform.GetChild(i).gameObject);
            }
        }
    }
}