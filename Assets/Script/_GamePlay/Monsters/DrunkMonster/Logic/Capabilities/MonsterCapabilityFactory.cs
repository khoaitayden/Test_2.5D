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
                .AddEffect<HasKilledPlayer>(EffectType.Increase) // The Goal
                .AddEffect<IsInvestigating>(EffectType.Decrease)
                
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1)
                
                // REQUIREMENT: Must be reachable to attack
                .AddCondition<IsPlayerReachable>(Comparison.GreaterThanOrEqual, 1) 
                
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 2. Taunt Player 
            builder.AddAction<TauntPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<IsPlayerReachable>(EffectType.Increase) 
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1)
                .AddCondition<IsPlayerReachable>(Comparison.SmallerThan, 1) 
                
                .SetBaseCost(2) 
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 3. Flee
            builder.AddAction<FleeAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase)
                .AddEffect<IsFleeing>(EffectType.Decrease) 
                .AddCondition<IsFleeing>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(2) 
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 4. Track Trace
            builder.AddAction<TrackTraceAction>()
                .SetTarget<FreshTraceTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddEffect<IsInvestigating>(EffectType.Decrease)
                
                .AddCondition<IsTrackingTrace>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThan, 1)
                .AddCondition<IsPlayerReachable>(Comparison.GreaterThanOrEqual, 1) 
                
                .SetBaseCost(3)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 5. Go To Last Seen
            builder.AddAction<GoToLastSeenPlayerAreaAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddCondition<IsInvestigating>(Comparison.GreaterThanOrEqual, 1) 
                .AddCondition<IsAtSuspiciousLocation>(Comparison.SmallerThan, 1) 
                .SetBaseCost(4)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 6. Search Surroundings
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<InvestigateTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddEffect<CanPatrol>(EffectType.Increase)     
                .AddCondition<IsInvestigating>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsAtSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(5)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 7. Patrol
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
            builder.AddWorldSensor<IsTrackingTraceSensor>().SetKey<IsTrackingTrace>();
            builder.AddWorldSensor<IsFleeingSensor>().SetKey<IsFleeing>();
            builder.AddWorldSensor<IsPlayerReachableSensor>().SetKey<IsPlayerReachable>();

            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PlayerLastSeenPosSensor>().SetTarget<PlayerLastSeenTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            builder.AddTargetSensor<InvestigateTargetSensor>().SetTarget<InvestigateTarget>();
            builder.AddTargetSensor<FreshTraceSensor>().SetTarget<FreshTraceTarget>();
            
            return builder.Build();
        }
    }
}