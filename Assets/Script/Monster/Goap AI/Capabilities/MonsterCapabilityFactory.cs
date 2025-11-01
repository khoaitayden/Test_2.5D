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
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // LEVEL 2: SEARCH THE AREA
            // This action searches cover points. It should have NO CONDITIONS.
            // It runs as long as there are suspicious locations to investigate.
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase)  // Might find player
                .AddEffect<HasSuspiciousLocation>(EffectType.Decrease) // Clear the clue when done
                .AddEffect<CanPatrol>(EffectType.Increase)  // Can patrol after search
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1); // Only condition: need a clue

            // LEVEL 3: GO TO THE CLUE
            // This action gets us to the search area FIRST.
            builder.AddAction<GoToLastSeenPlayerAreaAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase)
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsAtSuspiciousLocation>(Comparison.SmallerThan, 1); // NOT there yet

            // LEVEL 4: PATROL (THE DEFAULT ACTION)
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase) 
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1);

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