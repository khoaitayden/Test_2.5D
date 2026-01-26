using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Kidnap Config")] // Make it a ScriptableObject for easy tweaking
public class KidnapMonsterConfig : MonsterConfigBase
{
    [Header("Kidnap Specifics")]
    [Tooltip("How far the monster teleports the player when caught")]
    public float teleportDistance = 50f;

    [Tooltip("How much Player Energy to drain on kidnap (0.5 = 50%)")]
    public float energyDrainPercent = 0.5f;

    [Header("Ambush Logic")]
    [Tooltip("How close the monster gets before hiding")]
    public float hideDistance = 25f;

    [Header("Flee From Light")]
    public float fleeFromLightDistance = 30f;
}