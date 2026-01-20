using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class DirtyMonsterManager : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private int maxMonsters = 10;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private LayerMask groundLayer; 

    // Internal State
    private List<GameObject> _monsterPool = new List<GameObject>();
    private float _spawnTimer;

    void Awake()
    {
        InitializePool();
    }

    void OnEnable()
    {
        // 1. Spawn the first one immediately
        TrySpawnMonster();

        // 2. Reset timer so the next one comes after 'spawnInterval'
        _spawnTimer = 0f;
    }

    void OnDisable()
    {
        // 3. Disable all monsters when this manager is turned off
        foreach (var m in _monsterPool)
        {
            if (m != null && m.activeSelf)
            {
                m.SetActive(false);
            }
        }
    }

    void Update()
    {
        // Timer only runs while this component is Enabled
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= spawnInterval)
        {
            TrySpawnMonster();
            _spawnTimer = 0f;
        }
    }

    private void InitializePool()
    {
        // Prevent double init if enabled/disabled multiple times
        if (_monsterPool.Count > 0) return;

        for (int i = 0; i < maxMonsters; i++)
        {
            GameObject obj = Instantiate(monsterPrefab, transform.position, Quaternion.identity);
            
            if (obj.GetComponent<BehaviorGraphAgent>() == null)
            {
                Debug.LogError("DirtyMonster prefab is missing BehaviorGraphAgent!");
            }

            obj.SetActive(false);
            obj.transform.SetParent(this.transform); 
            _monsterPool.Add(obj);
        }
    }

    private void TrySpawnMonster()
    {
        GameObject monster = GetFreeMonster();
        
        if (monster == null) return;

        Vector3 spawnPos = FindNavMeshPosition();
        
        if (spawnPos != Vector3.zero)
        {
            monster.transform.position = spawnPos;
            monster.transform.rotation = Quaternion.identity;

            // Reset Logic
            var agent = monster.GetComponent<BehaviorGraphAgent>();
            if (agent != null)
            {
                agent.Restart();
            }

            monster.SetActive(true);
        }
    }

    private GameObject GetFreeMonster()
    {
        foreach (var m in _monsterPool)
        {
            if (!m.activeInHierarchy) return m;
        }
        return null;
    }

    private Vector3 FindNavMeshPosition()
    {
        for (int i = 0; i < 10; i++) 
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 attemptPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Raycast high up to find ground
            if (Physics.Raycast(attemptPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    return navHit.position;
                }
            }
        }
        return Vector3.zero; 
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}