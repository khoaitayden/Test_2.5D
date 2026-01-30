using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Kidnap Config")]
public class KidnapMonsterConfig : MonsterConfigBase
{
    [Header("Kidnap")]
    public float mapRadius = 100f; 
    public int teleportSampleAttempts = 20;
    
    public float energyDrainPercent = 0.5f;

    [Header("Data References")]
    public FloatVariableSO currentEnergy;
    public FloatVariableSO maxEnergy;
    public BoolVariableSO isCarryingItem;
    public TransformAnchorSO beaconAnchor;
    public TransformSetSO activeObjectivesSet;

    [Header("Ambush Logic")]
    public float findHideRadius = 25f;
    public float playerComeCloseFleeDistance = 10f;
    public float hideBehindCoverDuration = 5f;
}
