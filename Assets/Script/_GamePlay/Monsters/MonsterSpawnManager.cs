using UnityEngine;
using System.Collections.Generic;

public class MonsterSpawnManager : MonoBehaviour
{
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [System.Serializable]
    public struct MonsterMapping {
        public MonsterType type;
        public GameObject monsterObject; // Or Prefab if instantiating
    }

    [SerializeField] private List<MonsterMapping> monsters;

    void OnEnable()
    {
        objectiveEvents.OnAreaItemPickedUp += EnableMonster;
        // NEW LISTENER
        objectiveEvents.OnAreaReset += DisableMonster;
    }

    void OnDisable()
    {
        objectiveEvents.OnAreaItemPickedUp -= EnableMonster;
        // NEW LISTENER
        objectiveEvents.OnAreaReset -= DisableMonster;
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
                    m.monsterObject.SetActive(true);
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
                if(m.monsterObject != null) 
                {
                    m.monsterObject.SetActive(false);
                    Debug.Log($"[MonsterManager] Despawned {typeToDespawn} because Area {area.areaName} was reset.");
                }
            }
        }
    }
}