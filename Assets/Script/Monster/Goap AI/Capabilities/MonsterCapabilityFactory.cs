// FILE TO EDIT: MonsterCapabilityFactory.cs
// Add the new using statement for the keys/goals you created if they are in a different namespace.
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

            // --- PATROL LOGIC (Cost 5, Lowest Priority) ---
            builder.AddGoal<PatrolGoal>()
                .SetBaseCost(5f)
                .AddCondition<IsPatrol>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<PatrolAction>()
                .AddEffect<IsPatrol>(EffectType.Increase)
                .SetTarget<PatrolTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();

            // --- KILL PLAYER LOGIC (Cost 1, Highest Priority) ---
            builder.AddGoal<KillPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1);

            // #### NEW INVESTIGATE LOGIC (Cost 3, Medium Priority) ####
            builder.AddGoal<InvestigateGoal>() // Your new goal
                .SetBaseCost(3f)
                .AddCondition<HasInvestigated>(Comparison.GreaterThanOrEqual, 1); // Goal condition

            builder.AddAction<LookAroundAction>()
                .AddEffect<HasInvestigated>(EffectType.Increase) // This action satisfies the goal
                .AddCondition<AtLastSeenPlayerPosition>(Comparison.GreaterThanOrEqual, 1); // but requires us to BE there first.
                
            builder.AddAction<GoToLastSeenPlayerPositionAction>()
                .SetTarget<PlayerLastSeenTarget>() // This action needs the target
                .AddEffect<AtLastSeenPlayerPosition>(EffectType.Increase); // and its effect satisfies the LookAroundAction's condition.

            // --- SENSORS (Unchanged, just listed for context) ---
            builder.AddWorldSensor<PlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            
            // This is where you would register a PlayerLastSeenSensor, but we will handle it in the brain.

            return builder.Build();
        }
    }
}