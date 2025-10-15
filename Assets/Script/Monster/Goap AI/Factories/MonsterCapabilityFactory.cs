using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Agent.Core;
using UnityEngine;

public class MonsterCapabilityFactory : CapabilityFactoryBase
{
    public override ICapabilityConfig Create()
    {
        var builder = new CapabilityBuilder("MonsterCapability");

        // Goals
        builder.AddGoal<PatrolGoal>();
        builder.AddGoal<KillPlayerGoal>().SetBaseCost(2);

        // Actions
        builder.AddAction<FindPatrolPointAction>()
            .SetTarget<PatrolPointTarget>();

        // Require HasPatrolPoint >= 1 to MoveTo patrol point
        builder.AddAction<MoveToAction>()
            .SetTarget<PatrolPointTarget>()
            .AddCondition<HasPatrolPoint>(Comparison.GreaterThanOrEqual, 1);

        // If player in sight (value >= 1) -> move towards player
        builder.AddAction<MoveToAction>()
            .SetTarget<PlayerTarget>()
            .AddCondition<PlayerInSight>(Comparison.GreaterThanOrEqual, 1);

        // If player in attack range (value >= 2) -> attack
        builder.AddAction<AttackPlayerAction>()
            .SetTarget<PlayerTarget>()
            .AddCondition<PlayerInSight>(Comparison.GreaterThanOrEqual, 2);

        // Sensors
        builder.AddTargetSensor<MonsterTargetSensor>()
            .SetTarget<PatrolPointTarget>();

        // Make sure you have a world sensor class named PlayerWorldSensor (or change this to the sensor class you actually have)
        builder.AddTargetSensor<PlayerTargetSensor>();

        return builder.Build();
    }
}
