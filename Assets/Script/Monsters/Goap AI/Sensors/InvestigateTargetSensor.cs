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
            // Cache
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>();
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();

            if (brain == null || !brain.IsInvestigating || brain.LastKnownPlayerPosition == Vector3.zero)
            {
                coverFinder?.Clear();
                return null;
            }

            // --- THE SAFETY LOCK ---
            // If the queue is empty, we only generate if we are Physically Close to the LastKnownPos.
            // This prevents generation from triggering remotely while chasing or running towards the spot.
            if (!coverFinder.HasPoints)
            {
                float distanceToCenter = Vector3.Distance(agent.Transform.position, brain.LastKnownPlayerPosition);
                
                // Allow a reasonable radius (Stopping Dist + Buffer)
                if (distanceToCenter <= config.baseStoppingDistance + 3.0f) 
                {
                    // WE ARRIVED. Now generate points.
                    coverFinder.GeneratePoints(brain.LastKnownPlayerPosition, agent.Transform.position);
                }
                else
                {
                    // Too far away. Don't generate anything.
                    // This forces 'HasPoints' to stay False.
                    // Which forces 'IsAtSuspiciousLocationSensor' to check physical distance (False).
                    // Which forces Planner to pick 'GoToLastSeen'.
                    return null;
                }
            }

            // Return current point if available
            if (coverFinder.HasPoints)
            {
                return new PositionTarget(coverFinder.GetCurrentPoint());
            }

            // If we generated (tried) but found 0 points, return LastPos so SearchAction can gracefully finish
            return new PositionTarget(brain.LastKnownPlayerPosition);
        }
    }
}