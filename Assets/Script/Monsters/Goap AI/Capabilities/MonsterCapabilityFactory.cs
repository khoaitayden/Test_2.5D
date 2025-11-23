using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class MonsterCapabilityFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder("MonsterCapability");

            // --- GOAL ---
            builder.AddGoal<KillPlayerGoal>()
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);

            // --- ACTIONS ---

            //Attack player action
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(1);

            //Rushing to last seen position
            builder.AddAction<GoToLastSeenPlayerAreaAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase) 
                .AddCondition<IsInvestigating>(Comparison.GreaterThanOrEqual, 1) 
                .AddCondition<IsAtSuspiciousLocation>(Comparison.SmallerThan, 1) 
                .SetBaseCost(2);

            //Search for player action
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<PlayerLastSeenTarget>() 
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddEffect<CanPatrol>(EffectType.Increase)     
                .AddCondition<IsInvestigating>(Comparison.GreaterThanOrEqual, 1) 
                .AddCondition<IsAtSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1) 
                .SetBaseCost(3);
            //Patrol
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(10);

                // --- SENSORS ---
                builder.AddWorldSensor<PlayerInSightSensor>().SetKey<IsPlayerInSight>();
                builder.AddWorldSensor<HasSuspiciousLocationSensor>().SetKey<HasSuspiciousLocation>();
                builder.AddWorldSensor<IsAtSuspiciousLocationSensor>().SetKey<IsAtSuspiciousLocation>();
                builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
                builder.AddWorldSensor<IsInvestigatingSensor>().SetKey<IsInvestigating>();
                
                builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
                builder.AddTargetSensor<PlayerLastSeenSensor>().SetTarget<PlayerLastSeenTarget>();
                builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
                
                return builder.Build();
        }
    }
}