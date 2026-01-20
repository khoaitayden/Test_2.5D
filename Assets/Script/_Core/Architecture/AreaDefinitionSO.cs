using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Area Definition")]
public class AreaDefinitionSO : ScriptableObject
{
    public string areaName;
    public MissionItemSO associatedItem;

    public MonsterType associatedMonsterType; 
}

public enum MonsterType
{
    None,
    DrunkMonster,
    EyeMonster,
    KidnapMonster,
    DirtyMonster
}