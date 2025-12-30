using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class IsPlayerReachableSensor : LocalWorldSensorBase
    {
        private MonsterBrain brain;
        private NavMeshAgent agent;
        private NavMeshPath _path; // Reuse path object to save memory

        public override void Created() 
        {
            _path = new NavMeshPath();
        }

        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agentReceiver, IComponentReference references)
        {
            if (brain == null) brain = references.GetCachedComponent<MonsterBrain>();
            if (agent == null) agent = references.GetCachedComponent<NavMeshAgent>();

            // 1. If we don't see the player, reachability is irrelevant (default to true to avoid blocking)
            if (!brain.IsPlayerVisible || brain.CurrentPlayerTarget == null)
            {
                return 1;
            }

            // 2. Calculate Path
            Vector3 targetPos = brain.CurrentPlayerTarget.position;
            
            // Sample target position to ensure it's on/near NavMesh
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                agent.CalculatePath(hit.position, _path);

                // 3. Check Status
                // If path is Partial (blocked) or Invalid, they are Unreachable.
                if (_path.status != NavMeshPathStatus.PathComplete)
                {
                    return 0; // False: Unreachable
                }
            }
            else
            {
                return 0; // False: Player is off-mesh (flying/noclip)
            }

            return 1; // True: Reachable
        }
    }
}