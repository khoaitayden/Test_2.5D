using UnityEngine;
public class DrunkMonsterConfig : MonsterConfigBase
{

    [Header("Investigation")]
    public float investigateRadius = 50f;
    public int investigationPoints = 5;
    public float maxInvestigationTime = 20.0f;
    public float minCoverPointDistance = 20.0f; 
    public int numCoverFinderRayCasts = 16;
    

    [Header("Taunt Behavior")]
    public float idealTauntRange = 15f;
    public float minTauntRange = 8f;
    public float maxTauntRange = 25f;
    public float minTauntSpeed = 2f;
    public float maxTauntSpeed = 6f;
}