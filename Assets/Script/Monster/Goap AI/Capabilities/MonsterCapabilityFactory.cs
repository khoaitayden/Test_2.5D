using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class MonsterCapabilityFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder("MonsterCapability");

            // --- THE ONE AND ONLY GOAL ---
            builder.AddGoal<KillPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<HasKilledPlayer>(Comparison.GreaterThanOrEqual, 1);

            // --- THE ACTION TREE ---

            // LEVEL 1: KILL THE PLAYER
            // This is the final action in any successful plan.
            builder.AddAction<AttackPlayerAction>()
                .SetTarget<PlayerTarget>()
                .AddEffect<HasKilledPlayer>(EffectType.Increase)
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1); // Requires player to be visible.

            // LEVEL 2: FIND THE PLAYER (SEARCHING)
            // This is the action taken when we have a clue. It leads to finding the player.
            builder.AddAction<SearchSurroundingsAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddEffect<CanPatrol>(EffectType.Increase) // When search is over, we can patrol again.
                // Note: The "effect" isn't killing the player directly, but by searching,
                // we might re-acquire them, which satisfies the IsPlayerInSight condition for the Attack action.
                // The planner understands this indirect connection.
                .AddCondition<IsAtSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1);

            // LEVEL 3: GO TO THE CLUE
            // This action gets us to the search area.
            builder.AddAction<GoToLastSeenPositionAction>()
                .SetTarget<PlayerLastSeenTarget>()
                .AddEffect<IsAtSuspiciousLocation>(EffectType.Increase)
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<HasSuspiciousLocation>(Comparison.GreaterThanOrEqual, 1);

            // LEVEL 4: PATROL (THE DEFAULT ACTION)
            // This is the root action. The monster patrols to put itself in a state where it *might* see the player.
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<CanPatrol>(EffectType.Increase) 
                .AddEffect<IsPlayerInSight>(EffectType.Increase)
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1); // Requires the AI to be in its idle state.

            // --- SENSORS ---
            builder.AddWorldSensor<PlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<HasSuspiciousLocationSensor>().SetKey<HasSuspiciousLocation>();
            builder.AddWorldSensor<IsAtSuspiciousLocationSensor>().SetKey<IsAtSuspiciousLocation>();

            // We need a sensor for our new default state.
            // For now, we will just have it return true so patrolling can start.
            // A more advanced AI might have this be false during a stun, for example.
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PlayerLastSeenSensor>().SetTarget<PlayerLastSeenTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            
            return builder.Build();
        }
    }
}