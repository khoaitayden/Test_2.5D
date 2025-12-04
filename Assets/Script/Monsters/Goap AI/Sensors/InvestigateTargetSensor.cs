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
            // Cache references
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (coverFinder == null) coverFinder = references.GetCachedComponent<CoverFinder>();
            if (config == null) config = references.GetCachedComponent<MonsterConfig>();

            // If not investigating, reset and do nothing
            if (brain == null || !brain.IsInvestigating || brain.LastKnownPlayerPosition == Vector3.zero)
            {
                coverFinder?.Clear();
                return null;
            }

            // --- THE FIX: SAFETY LOCK ---
            // Only generate points if the queue is empty AND we are close to the target area.
            if (!coverFinder.HasPoints)
            {
                float distToLastSeen = Vector3.Distance(agent.Transform.position, brain.LastKnownPlayerPosition);
                
                // Allow a small buffer (e.g., 3.0m) around the destination.
                // If we are further than this, we assume we haven't arrived yet.
                // NOTE: We use baseStoppingDistance from config + 1.0f
                float arrivalThreshold = config.stoppingDistance + 1.0f;

                if (distToLastSeen > arrivalThreshold)
                {
                    // We are too far away. The 'GoToLastSeenPlayerAreaAction' needs to run first.
                    // Returning null prevents the 'SearchSurroundingsAction' from running.
                    return null;
                }

                // We have arrived! Now we can generate points.
                coverFinder.GeneratePoints(brain.LastKnownPlayerPosition, agent.Transform.position);
            }

            // If we have points (either just generated or remaining in queue), return the current one.
            if (coverFinder.HasPoints)
            {
                return new PositionTarget(coverFinder.GetCurrentPoint());
            }

            // If we generated points but found NONE (queue is still empty after generation),
            // return the LastKnownPosition as a fallback so the action can start and then finish gracefully.
            return new PositionTarget(brain.LastKnownPlayerPosition);
        }
    }
}