using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsAtSuspiciousLocationSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;
        private NavMeshAgent navMeshAgent;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            // 1. Cache References
            if (this.brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (this.navMeshAgent == null) navMeshAgent = references.GetCachedComponent<NavMeshAgent>();

            if (brain == null || navMeshAgent == null) return 0;
            if (brain.LastKnownPlayerPosition == Vector3.zero) return 0;

            // 2. Get Positions
            Vector3 currentPos = agent.Transform.position;
            Vector3 targetPos = brain.LastKnownPlayerPosition;

            // 3. FLATTEN THE Y AXIS (Height doesn't matter for "Area" checks)
            currentPos.y = 0;
            targetPos.y = 0;

            // 4. Calculate Horizontal Distance
            float flatDistance = Vector3.Distance(currentPos, targetPos);

            // 5. Threshold Calculation
            // We use StoppingDistance + a buffer.
            // Since investigation is an "Area" check, not a "Touch" check, 
            // 2.0f to 3.0f buffer is standard to prevent infinite adjusting.
            float threshold = navMeshAgent.stoppingDistance + 3.0f;

            // 6. Success Logic
            // Return 1 (True) if we are horizontally close enough
            if (flatDistance <= threshold)
            {
                return 1;
            }

            // 7. BACKUP: NavMeshAgent status check
            // If the Agent thinks it stopped because it hit a wall/end of path, 
            // accept that as "Arrived" so we don't loop forever.
            if (!navMeshAgent.pathPending && 
                navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f && 
                navMeshAgent.velocity.sqrMagnitude < 0.1f)
            {
                 // Check if the path was actually going to the target area?
                 // Simple logic: If we stopped moving, assume we arrived as best we could.
                 return 1; 
            }

            return 0;
        }
    }
}