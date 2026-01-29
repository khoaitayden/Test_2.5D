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

            // --- GOAL 1: KIDNAP ---
            builder.AddGoal<KidnapGoal>()
                .AddCondition<HasKidnappedPlayer>(Comparison.GreaterThanOrEqual, 1);

            // --- GOAL 2: FLEE ---
            builder.AddGoal<FleeGoal>()
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1);
            // --- GOAL 3: HIDE ---
            builder.AddGoal<HideGoal>()
                .AddCondition<IsHiding>(Comparison.GreaterThanOrEqual, 1);

            // --- ACTIONS ---

            // 1. FLEE ACTION
            builder.AddAction<FleeAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<IsFleeing>(EffectType.Decrease)
                .AddCondition<IsFleeing>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 2. KIDNAP ACTION
            builder.AddAction<KidnapAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKidnappedPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1) // Cannot kidnap while fleeing
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 3. HIDE
            builder.AddAction<HideAction>()
                .SetTarget<HideTarget>()
                .AddEffect<IsHiding>(EffectType.Increase)
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);
            
            // 4. TRACK TRACE ACTION
            builder.AddAction<TrackTraceAction>()
                .SetTarget<FreshTraceTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<IsTrackingTrace>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThan, 1)
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1)
                .SetBaseCost(3)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 5. PATROL ACTION
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(10)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // --- SENSORS ---
            builder.AddWorldSensor<IsPlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            builder.AddWorldSensor<IsFleeingSensor>().SetKey<IsFleeing>();
            builder.AddWorldSensor<IsTrackingTraceSensor>().SetKey<IsTrackingTrace>();
            builder.AddWorldSensor<IsLitByFlashlightSensor>().SetKey<IsLitByFlashlight>();
            builder.AddWorldSensor<IsHidingSensor>().SetKey<IsHiding>();

            // --- TARGET SENSORS ---
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            builder.AddTargetSensor<FreshTraceSensor>().SetTarget<FreshTraceTarget>();
            builder.AddTargetSensor<HideTargetSensor>().SetTarget<HideTarget>();
            
            return builder.Build();
        }
    }
}