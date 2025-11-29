using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateTargetSensor : LocalTargetSensorBase
    {
        private MonsterBrain brain;
        private CoverFinder coverFinder;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>();

            if (brain == null || !brain.IsInvestigating || brain.LastKnownPlayerPosition == Vector3.zero)
            {
                coverFinder?.Clear();
                return null;
            }

            // If queue empty, generate ONCE based on Last Known Pos
            if (!coverFinder.HasPoints)
            {
                coverFinder.GeneratePoints(brain.LastKnownPlayerPosition, agent.Transform.position);
            }

            // Return current point
            if (coverFinder.HasPoints)
            {
                return new PositionTarget(coverFinder.GetCurrentPoint());
            }

            // Fallback
            return new PositionTarget(brain.LastKnownPlayerPosition);
        }
    }
}