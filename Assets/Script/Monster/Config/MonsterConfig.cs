// In Assets/Monster/Config/MonsterConfig.cs
using UnityEngine;

public class MonsterConfig : MonoBehaviour
{
    [Header("Patrol")]
    public float MinPatrolDistance = 15f;
    public float MaxPatrolDistance = 50f;

    [Header("Attack")]
    public float ViewRadius = 20f;
    public LayerMask PlayerLayerMask;
    public float AttackDistance = 2f;

    [Header("Unstuck Logic")]
    public float MaxStuckTime = 5f;
    public float StuckVelocityThreshold = 0.1f;
}