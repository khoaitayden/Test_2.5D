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
                .AddCondition<IsPatrol>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThanOrEqual, 0)
                .AddCondition<HasSuspiciousLocation>(Comparison.SmallerThanOrEqual, 0);
            
            builder.AddAction<PatrolAction>()
                .AddEffect<IsPatrol>(EffectType.Increase)
                .SetTarget<PatrolTarget>()
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThanOrEqual, 0)
                .AddCondition<HasSuspiciousLocation>(Comparison.SmallerThanOrEqual, 0);;
        

            // --- KILL PLAYER LOGIC ---
            builder.AddGoal<KillPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // --- INVESTIGATE LOGIC ---
            builder.AddGoal<InvestigateGoal>()
                .SetBaseCost(3f)
                .AddCondition<HasInvestigated>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1);

            builder.AddAction<InvestigateLocationAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<HasInvestigated>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.SmallerThanOrEqual, 0);

            // --- SENSORS ---
            builder.AddWorldSensor<PlayerInSightSensor>()
                .SetKey<IsPlayerInSight>();
            
            // #### THIS IS THE FIX ####
            // We officially register our new key with the sensing system.
            builder.AddWorldSensor<HasSuspiciousLocationSensor>()
                .SetKey<HasSuspiciousLocation>();
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>()
                .SetTarget<PlayerTarget>();

            builder.AddTargetSensor<PlayerLastSeenSensor>()
                .SetTarget<PlayerLastSeenTarget>();
            
            builder.AddTargetSensor<PatrolTargetSensor>()
                .SetTarget<PatrolTarget>();
            
            return builder.Build();
        }
    }
}