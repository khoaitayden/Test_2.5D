using UnityEngine;
using System.Collections;

public class EyeMonsterManager : MonoBehaviour
{
    public static EyeMonsterManager Instance { get; private set; }
    [Header("Dependencies")]
    [SerializeField] private GameObject eyeObject;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private LightEnergyManager lightManager;

    [SerializeField] private float minSpawnInterval = 180f;
    [SerializeField] private float maxSpawnInterval = 240f;

    [Header("Spawn Chance (Light Dependent)")]
    [Range(0f, 1f)] [SerializeField] private float chanceAtFullLight = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float chanceAtNoLight = 0.40f;

    [Header("Spawn Radius (Light Dependent)")]
    [SerializeField] private float radiusAtFullLight = 30f;
    [SerializeField] private float radiusAtNoLight = 8f;

    [Header("Spawn Position")]
    [SerializeField] private float minSpawnHeight = 1.5f;
    [SerializeField] private float maxSpawnHeight = 4.0f; 

    // State
    private bool isUnlocked = false;
    public bool IsPlayerExposed { get; private set; }
    public Transform PlayerTransform => playerTransform;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    void Start()
    {
        if (eyeObject != null)
            eyeObject.SetActive(false);
        UnlockEyeSpawning(); 
    }
    public void UnlockEyeSpawning()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        Debug.Log("[EyeManager] Spawning has been unlocked. Starting timer.");
        StartCoroutine(SpawnTimer());
    }

    private IEnumerator SpawnTimer()
    {
        while (isUnlocked)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // After waiting, try to spawn
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        // 1. Check Preconditions
        if (eyeObject == null || playerTransform == null || lightManager == null)
        {
            Debug.LogError("[EyeManager] A dependency is not set!");
            return;
        }

        // If eye is already visible, do nothing.
        if (eyeObject.activeSelf) return;

        // 2. Calculate Chance based on Light
        float lightFraction = lightManager.EnergyFraction;
        // High light = Low chance. Low light = High chance.
        float currentSpawnChance = Mathf.Lerp(chanceAtNoLight, chanceAtFullLight, lightFraction);

        // 3. RNG Roll
        if (Random.value > currentSpawnChance)
        {
            Debug.Log($"[EyeManager] Spawn roll failed. (Rolled {Random.value:F2}, needed < {currentSpawnChance:F2})");
            return; // Failed the roll
        }

        // 4. Calculate Radius based on Light
        // High light = Far radius. Low light = Close radius.
        float currentRadius = Mathf.Lerp(radiusAtNoLight, radiusAtFullLight, lightFraction);

        // 5. Find a valid position
        Vector3 spawnPos = FindValidPosition(currentRadius);

        if (spawnPos != Vector3.zero)
        {
            // SUCCESS: Teleport and enable the eye
            eyeObject.transform.position = spawnPos;
            eyeObject.SetActive(true);
            Debug.Log($"<color=red>[EyeManager]</color> Eye spawned at {spawnPos} ({currentRadius:F1}m away).");
        }
        else
        {
            Debug.Log("[EyeManager] Spawn roll succeeded, but failed to find a valid position.");
        }
    }

    private Vector3 FindValidPosition(float radius)
    {
        // Try 15 times to find a good spot
        for (int i = 0; i < 15; i++)
        {
            // Pick a random direction
            float randomAngle = Random.Range(0f, 360f);
            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;

            // Calculate position on the circle
            Vector3 attemptPos = playerTransform.position + direction * radius;

            // Raycast down from the sky to find the ground
            if (Physics.Raycast(attemptPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            {
                // --- THE FIX IS HERE ---
                // Pick a random height within the specified range
                float randomHeight = Random.Range(minSpawnHeight, maxSpawnHeight);
                Vector3 finalPos = hit.point + Vector3.up * randomHeight;
                // -----------------------

                // Make sure we didn't spawn inside a wall (check from player TO eye)
                Vector3 toEye = finalPos - playerTransform.position;
                // We check against 90% of the distance to avoid hitting the player's own collider
                if (!Physics.Raycast(playerTransform.position, toEye.normalized, toEye.magnitude * 0.9f))
                {
                    return finalPos; // Path is clear!
                }
            }
        }
        return Vector3.zero; // Failed to find a valid spot
    }

    // You will call this later when the player "finds" the eye
    public void DespawnEye()
    {
        if (eyeObject != null)
            eyeObject.SetActive(false);
    }

    public void SetExposureState(bool isExposed)
    {
        IsPlayerExposed = isExposed;
    }
    
    // For testing in the editor
    [ContextMenu("Force Spawn Attempt")]
    private void ForceSpawn()
    {
        TrySpawn();
    }
}