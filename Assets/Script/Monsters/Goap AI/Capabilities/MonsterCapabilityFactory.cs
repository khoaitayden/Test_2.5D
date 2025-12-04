using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;

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

            // 1. Attack Player
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(1)
                // CRITICAL FIX: Run Perform() immediately so we can track the moving player
                .SetMoveMode(ActionMoveMode.PerformWhileMoving); 

            // 2. Search Surroundings
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<InvestigateTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddEffect<CanPatrol>(EffectType.Increase)     
                .AddCondition<IsInvestigating>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsAtSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(3)
                // CRITICAL FIX: Run Perform() immediately so we can handle timeouts/stuck checks
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 3. Go To Last Seen
            builder.AddAction<GoToLastSeenPlayerAreaAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddCondition<IsInvestigating>(Comparison.GreaterThanOrEqual, 1) 
                .AddCondition<IsAtSuspiciousLocation>(Comparison.SmallerThan, 1) 
                .SetBaseCost(2);

            // 4. Patrol
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(10)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // --- SENSORS ---
            builder.AddWorldSensor<IsPlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<IsAtSuspiciousLocationSensor>().SetKey<IsAtSuspiciousLocation>();
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            builder.AddWorldSensor<IsInvestigatingSensor>().SetKey<IsInvestigating>();
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PlayerLastSeenPosSensor>().SetTarget<PlayerLastSeenTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            builder.AddTargetSensor<InvestigateTargetSensor>().SetTarget<InvestigateTarget>();
            
            return builder.Build();
        }
    }
}