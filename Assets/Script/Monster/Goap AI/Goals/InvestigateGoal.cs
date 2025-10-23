// FILE TO RESTORE: InvestigateGoal.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class InvestigateGoal : GoalBase
    {
        // This method allows the goal to tell the planner if it's possible or not.
        public override float GetCost(IActionReceiver agent, IComponentReference references)
        {
            // If we don't have a valid last known position, this goal is IMPOSSIBLE.
            var brain = references.GetCachedComponent<MonsterBrain>();
            if (brain == null || brain.LastKnownPlayerPosition == Vector3.zero)
            {
                // Returning a huge value makes the planner ignore this goal completely.
                return float.MaxValue;
            }
            
            // If we have a valid position, return our normal, high-priority cost.
            return 3f;
        }
    }
}