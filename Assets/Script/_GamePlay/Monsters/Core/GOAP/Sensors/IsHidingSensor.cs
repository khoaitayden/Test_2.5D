using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsHidingSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)  
        {  
            var brain = references.GetCachedComponent<KidnapMonsterBrain>();
            if (brain.HasReachedCover == true)
            {
                return true;
            } else 
                return false;
            //return brain.HasReachedCover ? 1 : 0;
        }
    }
}