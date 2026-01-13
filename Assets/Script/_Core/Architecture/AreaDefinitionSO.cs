using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Area Definition")]
public class AreaDefinitionSO : ScriptableObject
{
    public string areaName;
    public MissionItemSO associatedItem;
    
    // The "Key" to know which monster to spawn
    // For now, we can just use a specific tag, prefab name, or enum. 
    // Let's use an Enum for clarity in the Monster Manager later.
    public MonsterType associatedMonsterType; 
}

public enum MonsterType
{
    None,
    DrunkMonster,
    EyeMonster,
    KidnapMonster,
    InsectMonster
}