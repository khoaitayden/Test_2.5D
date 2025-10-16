using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Docs.GettingStarted.Capabilities
{
    public class PatrolCapabilityFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder("PatrolCapability");

            builder.AddGoal<PatrolGoal>()
                .AddCondition<IsPatrol>(Comparison.GreaterThanOrEqual, 1)
                .SetBaseCost(2);

            builder.AddAction<PatrolAction>()
                .AddEffect<IsPatrol>(EffectType.Increase)
                .SetTarget<PatrolTarget>();

            builder.AddTargetSensor<PatrolTargetSensor>()
                .SetTarget<PatrolTarget>();
            
            return builder.Build();
        }
    }
}