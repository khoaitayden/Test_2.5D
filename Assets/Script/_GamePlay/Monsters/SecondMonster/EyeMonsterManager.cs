using UnityEngine;
using System.Collections;

public class EyeMonsterManager : MonoBehaviour
{

    [Header("Data & Events")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [SerializeField] private BoolVariableSO isPlayerExposed; 
    [SerializeField] private TransformAnchorSO playerAnchor;
    //[SerializeField] private GameEventSO unlockEvent;
    
    [Header("Dependencies")]
    [SerializeField] private GameObject eyeObject;

    [SerializeField] private float minSpawnInterval = 180f;
    [SerializeField] private float maxSpawnInterval = 240f;
    [Range(0f, 1f)] [SerializeField] private float chanceAtFullLight = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float chanceAtNoLight = 0.40f;
    [SerializeField] private float radiusAtFullLight = 30f;
    [SerializeField] private float radiusAtNoLight = 8f;
    [SerializeField] private float minSpawnHeight = 1.5f;
    [SerializeField] private float maxSpawnHeight = 4.0f; 

    private bool isUnlocked = false;
    void Awake()
    {
        // Reset state on load
        if (isPlayerExposed != null) isPlayerExposed.Value = false;
    }

    void Start()
    {
        if (eyeObject != null) eyeObject.SetActive(false);
    }

    // Public method called by the GameEventListener Component
    public void UnlockEyeSpawning()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        Debug.Log("[EyeManager] Unlocked via Event.");
        StartCoroutine(SpawnTimer());
    }

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

        float lightFraction = 0f;
        if (maxEnergy.Value > 0) lightFraction = currentEnergy.Value / maxEnergy.Value;

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
        if (playerAnchor == null || playerAnchor.Value == null) return Vector3.zero;

        Transform playerTransform = playerAnchor.Value;
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
        SetExposureState(false);
        if (eyeObject != null) eyeObject.SetActive(false);
    }
    
    public void SetExposureState(bool exposed)
    {
        if (isPlayerExposed != null) isPlayerExposed.Value = exposed;
        
    }

    [ContextMenu("Force Spawn Attempt")]
    private void ForceSpawn()
    {
        TrySpawn();
    }
}