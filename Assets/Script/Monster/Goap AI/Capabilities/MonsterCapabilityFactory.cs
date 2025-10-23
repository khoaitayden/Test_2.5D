using CrashKonijn.Goap.MonsterGen.Capabilities;
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
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);
            
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // --- INVESTIGATE LOGIC ---
            // The goal checks if there's a valid position via GetCost()
            // The action provides the effect to satisfy the goal
            builder.AddGoal<InvestigateGoal>()
                .SetBaseCost(3f)
                .AddCondition<HasInvestigated>(Comparison.GreaterThanOrEqual, 1);
            
            builder.AddAction<InvestigateLocationAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<HasInvestigated>(EffectType.Increase);

            // --- SENSORS ---
            builder.AddWorldSensor<PlayerInSightSensor>()
                .SetKey<IsPlayerInSight>();
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>()
                .SetTarget<PlayerTarget>();
            
            builder.AddTargetSensor<PlayerLastSeenSensor>()
                .SetTarget<PlayerLastSeenTarget>();
            
            return builder.Build();
        }
    }
}