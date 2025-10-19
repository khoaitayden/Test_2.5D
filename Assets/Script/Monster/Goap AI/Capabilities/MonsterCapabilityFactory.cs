using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
public class MonsterCapabilityFactory : CapabilityFactoryBase
{
public override ICapabilityConfig Create()
{
var builder = new CapabilityBuilder("MonsterCapability");
// --- PATROL LOGIC ---
        builder.AddGoal<PatrolGoal>()
            .SetBaseCost(5f)
            .AddCondition<IsPatrol>(Comparison.GreaterThanOrEqual, 1);
        
        builder.AddAction<PatrolAction>()
            .AddEffect<IsPatrol>(EffectType.Increase)
            .SetTarget<PatrolTarget>();
        
        builder.AddTargetSensor<PatrolTargetSensor>()
            .SetTarget<PatrolTarget>();

        // --- KILL PLAYER LOGIC ---
        // CRITICAL: No PlayerInSight condition on the goal!
        // Both goals are always valid, cost determines priority
        builder.AddGoal<KillPlayerGoal>()
            .SetBaseCost(1f)  // Lower cost = higher priority
            .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);
        
        builder.AddAction<AttackPlayerAction>()
            .SetTarget<PlayerTarget>()
            .AddEffect<HasKilledPlayer>(EffectType.Increase)
            .AddCondition<PlayerInSight>(Comparison.GreaterThanOrEqual, 1);  // Action requires player in sight

        // --- SENSORS ---
        // This sensor updates the PlayerInSight world state
        builder.AddWorldSensor<PlayerInSightSensor>()
            .SetKey<PlayerInSight>();
        
        // This sensor provides the PlayerTarget (player position)
        builder.AddTargetSensor<PlayerSensor>()
            .SetTarget<PlayerTarget>();
        
        return builder.Build();
    }
}
}
