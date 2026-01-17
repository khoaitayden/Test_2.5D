using UnityEngine;
using System.Collections.Generic;

public class MonsterSpawnManager : MonoBehaviour
{
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [System.Serializable]
    public struct MonsterMapping {
        public MonsterType type;
        public GameObject monsterObject; 
    }

    // Track active types so we don't try to disable things that aren't there
    private List<MonsterType> activeMonsters = new List<MonsterType>();
    
    [SerializeField] private List<MonsterMapping> monsters;

    void OnEnable()
    {
        if (objectiveEvents != null)
        {
            objectiveEvents.OnAreaItemPickedUp += EnableMonster;
            // Listen to the specific reset event again
            // This event is now delayed until Respawn because GameObjectiveManager controls it
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

    // Removed OnPlayerRespawn(). We don't need the generic signal anymore.

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
                    
                    if (activeMonsters.Contains(typeToDespawn))
                        activeMonsters.Remove(typeToDespawn);

                    Debug.Log($"[MonsterManager] Despawned {typeToDespawn} because Area {area.areaName} was reset.");
                }
            }
        }
    }
}