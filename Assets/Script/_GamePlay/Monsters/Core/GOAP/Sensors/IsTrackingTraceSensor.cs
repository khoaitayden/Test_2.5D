using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsTrackingTraceSensor : LocalWorldSensorBase
    {
        private FreshTraceSensor targetSensor = new FreshTraceSensor();

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            var target = targetSensor.Sense(agent, references, null);
            return (target != null) ? 1 : 0;
        }
    }
}