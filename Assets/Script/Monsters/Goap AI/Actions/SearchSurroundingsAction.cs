using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities; 
using System.Collections.Generic;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class SearchSurroundingsAction : GoapActionBase<SearchSurroundingsAction.Data>
    {
        private MonsterMovement movement; 
        private CoverFinder coverFinder;
        private MonsterConfig config;
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            movement = agent.GetComponent<MonsterMovement>();
            coverFinder = agent.GetComponent<CoverFinder>();
            config = agent.GetComponent<MonsterConfig>();
            brain = agent.GetComponent<MonsterBrain>();

            data.investigationStartTime = Time.time;
            data.isDone = false; // Initialize the flag
            
            // Handle 0 points case: if CoverFinder is empty, just finish.
            if (coverFinder != null && !coverFinder.HasPoints)
            {
                Debug.Log("[Search] No cover points found. Finishing investigation.");
                brain?.OnInvestigationFinished();
                data.isDone = true;
                return;
            }

            if (data.Target != null)
            {
                movement.GoTo(data.Target.Position, MonsterMovement.SpeedState.Investigate);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.isDone) return ActionRunState.Completed;

            // Global Timeout
            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                return ActionRunState.Completed; 
            }

            if (movement.HasArrivedOrStuck())
            {
                // Arrived at current point.
                // The End() method calls coverFinder.AdvanceQueue() to setup the next point.
                // We complete this action so the Planner can re-evaluate and pick Search again for the next point.
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();

            if (coverFinder != null)
            {
                // Move to next point in queue
                coverFinder.AdvanceQueue();

                // If queue is empty now, we are done with the whole investigation
                if (!coverFinder.HasPoints)
                {
                    Debug.Log("[Search] All points checked. Investigation Finished.");
                    brain?.OnInvestigationFinished();
                }
            }
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public bool isDone; // FIELD ADDED
        }
    }
}