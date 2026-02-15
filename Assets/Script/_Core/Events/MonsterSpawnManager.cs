using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; // Needed for NavMeshAgent reset

public class MonsterSpawnManager : MonoBehaviour
{
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [System.Serializable]
    public struct MonsterMapping {
        public MonsterType type;
        public GameObject monsterObject; 
    }

    [SerializeField] private List<MonsterMapping> monsters;

    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();

    private List<MonsterType> activeMonsters = new List<MonsterType>();

    void Awake()
    {
        foreach(var m in monsters)
        {
            if(m.monsterObject != null)
            {
                originalPositions[m.monsterObject] = m.monsterObject.transform.position;
                originalRotations[m.monsterObject] = m.monsterObject.transform.rotation;
            }
        }
    }

    void OnEnable()
    {
        if (objectiveEvents != null)
        {
            objectiveEvents.OnAreaItemPickedUp += EnableMonster;
            objectiveEvents.OnAreaReset += DisableMonster; 
        }
    }

    void OnDisable()
    {
        if (objectiveEvents != null)
        {
            objectiveEvents.OnAreaItemPickedUp -= EnableMonster;
            objectiveEvents.OnAreaReset -= DisableMonster;
        }
    }

    private void EnableMonster(AreaDefinitionSO area)
    {
        MonsterType typeToSpawn = area.associatedMonsterType;

        foreach(var m in monsters)
        {
            if (m.type == typeToSpawn)
            {
                if(m.monsterObject != null) 
                {
                    // Reset Position BEFORE enabling
                    ResetMonsterPosition(m.monsterObject);
                    
                    m.monsterObject.SetActive(true);
                    
                    if (!activeMonsters.Contains(typeToSpawn))
                        activeMonsters.Add(typeToSpawn);
                        
                    Debug.Log($"Spawned {typeToSpawn} for area {area.areaName}");
                }
            }
        }
    }

    private void DisableMonster(AreaDefinitionSO area)
    {
        MonsterType typeToDespawn = area.associatedMonsterType;

        foreach(var m in monsters)
        {
            if (m.type == typeToDespawn)
            {
                if(m.monsterObject != null && m.monsterObject.activeSelf) 
                {
                    m.monsterObject.SetActive(false);
                    
                    // Reset Position AFTER disabling (Safety)
                    ResetMonsterPosition(m.monsterObject);

                    if (activeMonsters.Contains(typeToDespawn))
                        activeMonsters.Remove(typeToDespawn);

                    Debug.Log($"[MonsterManager] Despawned and Reset {typeToDespawn}.");
                }
            }
        }
    }

    private void ResetMonsterPosition(GameObject monster)
    {
        if (!originalPositions.ContainsKey(monster)) return;

        Vector3 startPos = originalPositions[monster];
        Quaternion startRot = originalRotations[monster];

        NavMeshAgent agent = monster.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(startPos);
            monster.transform.rotation = startRot;
            agent.velocity = Vector3.zero;
            return;
        }

        CharacterController controller = monster.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            monster.transform.position = startPos;
            monster.transform.rotation = startRot;
            controller.enabled = true;
            return;
        }

        monster.transform.position = startPos;
        monster.transform.rotation = startRot;
    }
}