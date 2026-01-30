using UnityEngine;
using System.Collections;

public class EyeMonsterManager : MonoBehaviour
{
    [Header("Data & Events")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [SerializeField] private BoolVariableSO isPlayerExposed; 
    [SerializeField] private TransformAnchorSO playerAnchor;
    [SerializeField] private IntVariableSO monstersWatchingCount;
    
    [Header("Dependencies")]
    [SerializeField] private GameObject eyeObject;

    [Header("Timings")]
    [SerializeField] private float firstSpawnDelay = 2.0f;
    [SerializeField] private float minSpawnInterval = 180f;
    [SerializeField] private float maxSpawnInterval = 240f;

    [Header("Spawn Chance")]
    [Range(0f, 1f)] [SerializeField] private float chanceAtFullLight = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float chanceAtNoLight = 0.40f;
    [SerializeField] private float radiusAtFullLight = 30f;
    [SerializeField] private float radiusAtNoLight = 8f;
    [SerializeField] private float minSpawnHeight = 1.5f;
    [SerializeField] private float maxSpawnHeight = 4.0f;

    [Header("Collision Safety")]
    [SerializeField] private LayerMask obstacleLayers; 
    [SerializeField] private float spawnSafetyRadius = 0.8f;

    private bool isUnlocked = false;
    private bool _hasSpawnedOnce = false;

    void OnEnable()
    {
        _hasSpawnedOnce = false;
        UnlockEyeSpawning();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        DespawnEye();
    }

    public void UnlockEyeSpawning()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        StartCoroutine(SpawnTimer());
    }

    private IEnumerator SpawnTimer()
    {
        if (!_hasSpawnedOnce)
        {
            yield return new WaitForSeconds(firstSpawnDelay);

            while (!_hasSpawnedOnce && isUnlocked)
            {
                // Force = true (Skip RNG check)
                bool success = TrySpawn(forceSpawn: true);
                
                if (success)
                {
                    _hasSpawnedOnce = true;
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }

        // --- PHASE 2: NORMAL RNG LOOP ---
        while (isUnlocked)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Force = false (Respect RNG check)
            TrySpawn(forceSpawn: false);
        }
    }

    private bool TrySpawn(bool forceSpawn)
    {
        if (eyeObject.activeSelf) return false;

        float lightFraction = 0f;
        if (maxEnergy.Value > 0) lightFraction = currentEnergy.Value / maxEnergy.Value;
        if (!forceSpawn)
        {
            float currentSpawnChance = Mathf.Lerp(chanceAtNoLight, chanceAtFullLight, lightFraction);
            if (Random.value > currentSpawnChance) return false; // RNG Failed
        }

        float currentRadius = Mathf.Lerp(radiusAtNoLight, radiusAtFullLight, lightFraction);
        Vector3 spawnPos = FindValidPosition(currentRadius);

        if (spawnPos != Vector3.zero)
        {
            eyeObject.transform.position = spawnPos;
            eyeObject.SetActive(true);
            if (monstersWatchingCount != null) monstersWatchingCount.ApplyChange(1);
            return true;
        }

        return false; 
    }

    private Vector3 FindValidPosition(float radius)
    {
        if (playerAnchor == null || playerAnchor.Value == null) return Vector3.zero;
        Transform playerTx = playerAnchor.Value;

        for (int i = 0; i < 15; i++)
        {
            float randomAngle = Random.Range(0f, 360f);
            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
            Vector3 attemptPos = playerTx.position + direction * radius;

            if (Physics.Raycast(attemptPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            {
                float randomHeight = Random.Range(minSpawnHeight, maxSpawnHeight);
                Vector3 finalPos = hit.point + Vector3.up * randomHeight;

                if (Physics.CheckSphere(finalPos, spawnSafetyRadius, obstacleLayers))
                {
                    continue; 
                }

                Vector3 toEye = finalPos - playerTx.position;
                if (!Physics.Raycast(playerTx.position, toEye.normalized, toEye.magnitude * 0.9f))
                {
                    return finalPos; 
                }
            }
        }
        return Vector3.zero; 
    }

    public void DespawnEye()
    {
        SetExposureState(false);
        
        if (eyeObject != null && eyeObject.activeSelf)
        {
            eyeObject.SetActive(false);
            if (monstersWatchingCount != null) monstersWatchingCount.ApplyChange(-1);
        }
    }
    
    public void SetExposureState(bool exposed)
    {
        if (isPlayerExposed != null) isPlayerExposed.Value = exposed;
    }

    [ContextMenu("Force Spawn Attempt")]
    private void ForceSpawn()
    {
        TrySpawn(true);
    }
}