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
            // The ONLY goal. Everything else supports this.
            builder.AddGoal<KidnapGoal>()
                .AddCondition<HasKidnappedPlayer>(Comparison.GreaterThanOrEqual, 1);

            // --- ACTIONS ---

            // 1. KIDNAP (The Win Condition)
            builder.AddAction<KidnapAction>()
                .SetTarget<PlayerTarget>()
                // EFFECT: This achieves the Goal
                .AddEffect<HasKidnappedPlayer>(EffectType.Increase) 
                // PRECONDITIONS:
                .AddCondition<IsPlayerInSight>(Comparison.GreaterThanOrEqual, 1) // Must see player
                .AddCondition<IsFleeing>(Comparison.SmallerThan, 1)              // Cannot be fleeing
                .SetBaseCost(1)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 2. PATROL (The Searcher)
            // Used when we don't see the player yet.
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                // EFFECT: Patrolling helps us find the player
                .AddEffect<IsPlayerInSight>(EffectType.Increase) 
                // PRECONDITIONS:
                .AddCondition<CanPatrol>(Comparison.GreaterThanOrEqual, 1)       // Must be allowed to patrol
                .SetBaseCost(10)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // 3. FLEE (The Recovery)
            // Used when "CanPatrol" is false because we are stuck/fleeing.
            builder.AddAction<FleeAction>()
                .SetTarget<PlayerTarget>()
                // EFFECT: Fleeing restores our ability to Patrol
                .AddEffect<CanPatrol>(EffectType.Increase)      
                .AddEffect<IsFleeing>(EffectType.Decrease)      
                // PRECONDITIONS:
                .AddCondition<IsFleeing>(Comparison.GreaterThanOrEqual, 1)       // Only runs if Brain says Flee
                .SetBaseCost(2)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving);

            // --- SENSORS ---
            builder.AddWorldSensor<IsPlayerInSightSensor>().SetKey<IsPlayerInSight>();
            builder.AddWorldSensor<CanPatrolSensor>().SetKey<CanPatrol>();
            builder.AddWorldSensor<IsFleeingSensor>().SetKey<IsFleeing>();
            // Add Hit/Touch sensor if needed for logic, though KidnapAction handles the trigger
            
            builder.AddTargetSensor<PlayerCurrentPosSensor>().SetTarget<PlayerTarget>();
            builder.AddTargetSensor<PatrolTargetSensor>().SetTarget<PatrolTarget>();
            
            return builder.Build();
        }
    }
}