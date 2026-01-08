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
        private MonsterConfig config;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>();
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();

            if (brain == null || !brain.IsInvestigating || brain.LastKnownPlayerPosition == Vector3.zero)
            {
                coverFinder?.Clear();
                return null;
            }

            // --- GENERATION LOGIC ---
            if (!coverFinder.HasPoints)
            {
                float distanceToCenter = Vector3.Distance(agent.Transform.position, brain.LastKnownPlayerPosition);
                
                // FIX: Match IsAtSuspiciousLocationSensor.
                // If we are inside the radius, we can start generating points.
                if (distanceToCenter <= config.investigateRadius) 
                {
                    coverFinder.GeneratePoints(brain.LastKnownPlayerPosition, agent.Transform.position);
                }
                else
                {
                    // Too far away. Force 'GoToLastSeenPlayerAreaAction'.
                    return null;
                }
            }

            if (coverFinder.HasPoints)
            {
                return new PositionTarget(coverFinder.GetCurrentPoint());
            }

            return new PositionTarget(brain.LastKnownPlayerPosition);
        }
    }
}