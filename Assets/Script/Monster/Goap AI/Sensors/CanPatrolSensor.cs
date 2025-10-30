using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen
{
    public class CanPatrolSensor : LocalWorldSensorBase
    {
        public override void Created()
        {
        }

        // For now, this sensor always returns true, meaning the monster is always
        // capable of patrolling if no other higher-priority action is available.
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            return 1;
        }

        public override void Update()
        {
        }
    }
}