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
            builder.AddGoal<KillPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<PlayerInSight>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<PlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // CORRECTED: We only have one sensor for the player.
            // The framework automatically knows that this sensor updates world state
            // because it calls the SetState method.
            builder.AddTargetSensor<PlayerSensor>()
                .SetTarget<PlayerTarget>();
            
            return builder.Build();
        }
    }
}