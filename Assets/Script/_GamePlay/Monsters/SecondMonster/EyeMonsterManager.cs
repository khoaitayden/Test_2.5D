using UnityEngine;
using System.Collections;

public class EyeMonsterManager : MonoBehaviour
{
    public static EyeMonsterManager Instance { get; private set; }
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [Header("Dependencies")]
    [SerializeField] private GameObject eyeObject;
    [SerializeField] private Transform playerTransform;

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
    // New property so monsters can check exposure status easily
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
            
    }

    public void UnlockEyeSpawning()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        Debug.Log("[EyeManager] First item placed. Eye Spawning has started.");
        StartCoroutine(SpawnTimer());
    }

    // ... (The rest of the script: SpawnTimer, TrySpawn, etc. remains exactly the same) ...
    
    private IEnumerator SpawnTimer()
    {
        while (isUnlocked)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        if (eyeObject.activeSelf) return;

        float lightFraction = currentEnergy.Value / maxEnergy.Value;
        float currentSpawnChance = Mathf.Lerp(chanceAtNoLight, chanceAtFullLight, lightFraction);

        if (Random.value > currentSpawnChance) return;

        float currentRadius = Mathf.Lerp(radiusAtNoLight, radiusAtFullLight, lightFraction);
        Vector3 spawnPos = FindValidPosition(currentRadius);

        if (spawnPos != Vector3.zero)
        {
            eyeObject.transform.position = spawnPos;
            eyeObject.SetActive(true);
        }
    }

    private Vector3 FindValidPosition(float radius)
    {
        for (int i = 0; i < 15; i++)
        {
            float randomAngle = Random.Range(0f, 360f);
            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
            Vector3 attemptPos = playerTransform.position + direction * radius;

            if (Physics.Raycast(attemptPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            {
                float randomHeight = Random.Range(minSpawnHeight, maxSpawnHeight);
                Vector3 finalPos = hit.point + Vector3.up * randomHeight;
                Vector3 toEye = finalPos - playerTransform.position;
                
                if (!Physics.Raycast(playerTransform.position, toEye.normalized, toEye.magnitude * 0.9f))
                {
                    return finalPos; 
                }
            }
        }
        return Vector3.zero; 
    }

    public void DespawnEye()
    {
        IsPlayerExposed = false;
        if (eyeObject != null) eyeObject.SetActive(false);
    }
    
    public void SetExposureState(bool exposed)
    {
        IsPlayerExposed = exposed;
    }

    [ContextMenu("Force Spawn Attempt")]
    private void ForceSpawn()
    {
        TrySpawn();
    }
}