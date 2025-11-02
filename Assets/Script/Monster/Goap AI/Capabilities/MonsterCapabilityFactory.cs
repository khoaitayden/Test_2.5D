using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class MonsterCapabilityFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder("MonsterCapability");

            // --- THE ONE AND ONLY GOAL ---
            builder.AddGoal<KillPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);

            // --- THE ACTION TREE ---

            // LEVEL 1: KILL THE PLAYER
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(1);

            // LEVEL 2: SEARCH THE AREA
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddEffect<CanPatrol>(EffectType.Increase)
                .AddCondition<IsAtSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1) // MUST be at the location
                .SetBaseCost(3);

            // LEVEL 3: GO TO THE CLUE
            builder.AddAction<GoToLastSeenPlayerAreaAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1) // MUST have a clue
                .AddCondition<IsAtSuspiciousLocation>(Comparison.SmallerThan, 1) 
                .SetBaseCost(2);

            // LEVEL 4: PATROL (THE DEFAULT ACTION)
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<HasSuspiciousLocation>(Comparison.SmallerThan, 1)
                .AddCondition<IsAtSuspiciousLocation>(Comparison.SmallerThan, 1) 
                .SetBaseCost(10);

            // --- SENSORS ---
            builder.AddWorldSensor<PlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<HasSuspiciousLocationSensor>().SetKey<HasSuspiciousLocation>();
            builder.AddWorldSensor<IsAtSuspiciousLocationSensor>().SetKey<IsAtSuspiciousLocation>();
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PlayerLastSeenSensor>().SetTarget<PlayerLastSeenTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            
            return builder.Build();
        }
    }
}