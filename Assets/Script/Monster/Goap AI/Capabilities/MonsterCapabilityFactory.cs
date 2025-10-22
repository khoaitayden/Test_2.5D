// FILE TO EDIT: MonsterCapabilityFactory.cs
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

            // --- PATROL LOGIC --- (unchanged)
            builder.AddGoal<PatrolGoal>().SetBaseCost(5f).AddCondition<IsPatrol>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<PatrolAction>().AddEffect<IsPatrol>(EffectType.Increase).SetTarget<PatrolTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();

            // --- KILL PLAYER LOGIC --- (unchanged)
            builder.AddGoal<KillPlayerGoal>().SetBaseCost(1f).AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<AttackPlayerAction>().SetTarget<PlayerTarget>().AddEffect<HasKilledPlayer>(EffectType.Increase).AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // --- INVESTIGATE LOGIC --- (unchanged)
            builder.AddGoal<InvestigateGoal>().SetBaseCost(3f).AddCondition<HasInvestigated>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<LookAroundAction>().AddEffect<HasInvestigated>(EffectType.Increase).AddCondition<AtLastSeenPlayerPosition>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<GoToLastSeenPlayerPositionAction>().SetTarget<PlayerLastSeenTarget>().AddEffect<AtLastSeenPlayerPosition>(EffectType.Increase);

            // --- SENSORS ---
            builder.AddWorldSensor<PlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            
            // #### THIS IS THE CRITICAL NEW LINE ####
            // We now tell the planner how to get the 'PlayerLastSeenTarget'.
            builder.AddTargetSensor<PlayerLastSeenSensor>().SetTarget<PlayerLastSeenTarget>();
            
            return builder.Build();
        }
    }
}