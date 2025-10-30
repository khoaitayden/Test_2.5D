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
            // Cache components for efficiency
            if (this.brain == null)
                this.brain = references.GetCachedComponent<MonsterBrain>();
            
            if (this.navMeshAgent == null)
                this.navMeshAgent = references.GetCachedComponent<NavMeshAgent>();

            if (this.brain == null || this.navMeshAgent == null)
                return 0;

            // If there's no suspicious location stored, we can't be at it.
            if (this.brain.LastKnownPlayerPosition == Vector3.zero)
                return 0;

            // The "fact" is true if our distance to the target position is less than
            // our stopping distance. This is a reliable way to check for "arrival".
            float distance = Vector3.Distance(agent.Transform.position, this.brain.LastKnownPlayerPosition);
            
            bool isAtLocation = distance <= this.navMeshAgent.stoppingDistance + 0.5f;

            return isAtLocation ? 1 : 0;
        }
    }
}