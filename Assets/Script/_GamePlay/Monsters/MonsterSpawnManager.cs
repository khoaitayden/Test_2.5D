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
    private List<MonsterType> activeMonsters = new List<MonsterType>();
    [SerializeField] private List<MonsterMapping> monsters;

    void OnEnable()
    {
        if (objectiveEvents != null)
            objectiveEvents.OnAreaItemPickedUp += EnableMonster;
    }

    void OnDisable()
    {
        if (objectiveEvents != null)
            objectiveEvents.OnAreaItemPickedUp -= EnableMonster;
    }
    public void OnPlayerRespawn()
    {
        DisableAllActiveMonsters();
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
                    
                    // Track it
                    if (!activeMonsters.Contains(typeToSpawn))
                        activeMonsters.Add(typeToSpawn);
                        
                    Debug.Log($"Spawned {typeToSpawn} for area {area.areaName}");
                }
            }
        }
    }

    private void DisableAllActiveMonsters()
    {
        foreach (var type in activeMonsters)
        {
            foreach(var m in monsters)
            {
                if (m.type == type && m.monsterObject != null)
                {
                    m.monsterObject.SetActive(false);
                }
            }
        }
        activeMonsters.Clear();
        Debug.Log("[MonsterManager] All monsters cleared on respawn.");
    }
}