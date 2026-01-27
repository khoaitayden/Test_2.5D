using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Kidnap Config")]
public class KidnapMonsterConfig : MonsterConfigBase
{
    [Header("Kidnap Specifics")]
    [Tooltip("Radius of your game map from (0,0,0). Used to find random valid points.")]
    public float mapRadius = 100f; 
    [Tooltip("How many random points to check to find the furthest one. Higher = Better results but more CPU.")]
    public int teleportSampleAttempts = 20;
    
    public float energyDrainPercent = 0.5f;

    [Header("Data References")]
    public FloatVariableSO currentEnergy;
    public FloatVariableSO maxEnergy;
    public BoolVariableSO isCarryingItem;
    public TransformAnchorSO beaconAnchor;
    public TransformSetSO activeObjectivesSet;

    [Header("Ambush Logic")]
    public float hideDistance = 25f;
    public float fleeFromLightDistance = 30f;
}