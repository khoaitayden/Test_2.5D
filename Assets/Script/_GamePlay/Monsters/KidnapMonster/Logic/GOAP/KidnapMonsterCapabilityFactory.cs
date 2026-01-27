using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class KidnapMonsterCapabilityFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder("KidnapMonsterCapability");

            // --- GOAL ---
            builder.AddGoal<KidnapGoal>()
                .AddCondition<HasKidnappedPlayer>(Comparison.GreaterThanOrEqual, 1);

            // --- ACTIONS ---
            // 1. KIDNAP
            builder.AddAction<KidnapAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKidnappedPlayer>(EffectType.Increase) 
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1)
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 2. TRACK TRACE
            builder.AddAction<TrackTraceAction>()
                .SetTarget<FreshTraceTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<IsTrackingTrace>(Comparison.GreaterThanOrEqual, 1) 
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThan, 1)        
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1)              
                .SetBaseCost(3)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 3. PATROL
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)     
                .SetBaseCost(10)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 4. FLEE
            builder.AddAction<FleeAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase)      
                .AddEffect<IsFleeing>(EffectType.Decrease)      
                .AddCondition<IsFleeing>(Comparison.GreaterThanOrEqual, 1)       
                .SetBaseCost(2)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            builder.AddWorldSensor<IsPlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            builder.AddWorldSensor<IsFleeingSensor>().SetKey<IsFleeing>();
            builder.AddWorldSensor<IsTrackingTraceSensor>().SetKey<IsTrackingTrace>();

            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            builder.AddTargetSensor<FreshTraceSensor>().SetTarget<FreshTraceTarget>();
            
            return builder.Build();
        }
    }
}