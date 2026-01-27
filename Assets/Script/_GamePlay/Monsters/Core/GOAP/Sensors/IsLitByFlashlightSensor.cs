using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsLitByFlashlightSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            var vision = references.GetCachedComponent<MonsterVision>();
            if (vision == null) return 0;

            return vision.IsLit ? 1 : 0;
        }
    }
}