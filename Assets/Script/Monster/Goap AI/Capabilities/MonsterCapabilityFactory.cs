using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class MonsterCapabilityFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder("MonsterCapability");

            // --- PATROL LOGIC --- (Unchanged)
            builder.AddGoal<PatrolGoal>()
                .SetBaseCost(5f)
                .AddCondition<IsPatrol>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThanOrEqual, 0)
                .AddCondition<HasSuspiciousLocation>(Comparison.SmallerThanOrEqual, 0);
            
            builder.AddAction<PatrolAction>()
                .AddEffect<IsPatrol>(EffectType.Increase)
                .SetTarget<PatrolTarget>()
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThanOrEqual, 0)
                .AddCondition<HasSuspiciousLocation>(Comparison.SmallerThanOrEqual, 0);
            
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();

            // --- KILL PLAYER LOGIC --- (Unchanged)
            builder.AddGoal<KillPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // --- REVISED INVESTIGATE LOGIC ---
            builder.AddGoal<InvestigateGoal>()
                .SetBaseCost(3f)
                .AddCondition<HasInvestigated>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1);

            // Action 1 - Go to the location
            builder.AddAction<GoToLastSeenPositionAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase)
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1);

            // Action 2 - Search the location
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<HasInvestigated>(EffectType.Increase)
                .AddCondition<IsAtSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1);

            // --- SENSORS --- (Unchanged)
            builder.AddWorldSensor<PlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<HasSuspiciousLocationSensor>().SetKey<HasSuspiciousLocation>();
            builder.AddWorldSensor<IsAtSuspiciousLocationSensor>().SetKey<IsAtSuspiciousLocation>();
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PlayerLastSeenSensor>().SetTarget<PlayerLastSeenTarget>();
            
            return builder.Build();
        }
    }
}