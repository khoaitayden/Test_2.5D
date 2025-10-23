using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateGoal : GoalBase
    {
        public override float GetCost(IActionReceiver agent, IComponentReference references)
        {
            // Only allow investigation if we actually have a last seen position
            var brain = references.GetCachedComponent<MonsterBrain>();
            
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero)
            {
                // No valid position to investigate - make this goal impossible
                return float.MaxValue;
            }
            
            // Valid investigation target
            return 3f; // Normal cost
        }
    }
}