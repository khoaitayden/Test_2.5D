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

            // 1. KIDNAP (The "Attack")
            builder.AddAction<KidnapAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKidnappedPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 2. PATROL (Re-used from Core)
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase) // Can see player while patrolling
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(10)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);
            
            // 3. FLEE (Re-used from Core, uses flee stats in MonsterConfigBase)
            builder.AddAction<FleeAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase)
                .AddCondition<IsFleeing>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(2)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // --- SENSORS (All re-used from Core) ---
            builder.AddWorldSensor<IsPlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            builder.AddWorldSensor<IsFleeingSensor>().SetKey<IsFleeing>();
            // Add more as needed (IsLitByFlashlight, etc.)
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            
            return builder.Build();
        }
    }
}