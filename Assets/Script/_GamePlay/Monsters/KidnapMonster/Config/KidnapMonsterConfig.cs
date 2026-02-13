using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Kidnap Config")]
public class KidnapMonsterConfig : MonsterConfigBase
{
    [Header("Kidnap")]
    public float mapRadius = 100f; 
    public float grabPreparationDistance=4f;
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
    public float playerComeCloseKidnapDistance = 10f;
    public float hideBehindCoverDuration = 5f;
    public float nervousThreshold = 0.75f;
    public float rotateHidingSpeed=2.0f;
    public float hideDistanceBehindTree = 2.5f;
    public float hideLookSpeed = 5.0f;
    public float treeDetectionRadius = 3.0f;

    [Header("Light Expose Sensitivity Settings")]
    public float lightToleranceDuration = 2.0f;
    public float lightDecaySpeed = 1.0f; 
}
