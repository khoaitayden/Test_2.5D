using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class HideTargetSensor : LocalTargetSensorBase
    {
        private KidnapHideFinder finder;
        private MonsterBrain brain;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (finder == null) finder = references.GetCachedComponent<KidnapHideFinder>();
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();

            // Ensure Finder knows where player is
            if (brain != null && brain.PlayerAnchor != null && brain.PlayerAnchor.Value != null)
            {
                finder.SetPlayer(brain.PlayerAnchor.Value);
            }

            Vector3? spot = finder.FindBestHideSpot();
            
            if (spot.HasValue)
            {
                return new PositionTarget(spot.Value);
            }
            Debug.Log("Position not found");
            return null;
        }
    }
}