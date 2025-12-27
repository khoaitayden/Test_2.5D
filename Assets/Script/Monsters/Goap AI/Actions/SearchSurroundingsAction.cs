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
            data.isDone = false; 
            
            // Handle 0 points case
            if (coverFinder != null && !coverFinder.HasPoints)
            {
                brain?.OnInvestigationFinished();
                data.isDone = true;
                return;
            }

            if (data.Target != null)
            {
                // FIX: Use MoveTo with explicit params (Using slower search speed)
                movement.MoveTo(data.Target.Position, config.investigateSpeed);
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

            // --- CHECK STATUS ---
            if (movement.HasArrivedOrStuck())
            {
                // DETECT STUCK VS ARRIVED
                // If we stopped but are still far from target, we are stuck.
                float dist = Vector3.Distance(agent.Transform.position, data.Target.Position);
                
                // If we are further than 3m from the cover point when we stopped...
                if (dist > 3.0f) 
                {
                    Debug.LogWarning($"[Search] Stuck {dist:F1}m away from cover. Triggering Flee.");
                    brain?.OnMovementStuck();
                    return ActionRunState.Stop; 
                }

                // Otherwise, normal arrival.
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();

            if (coverFinder != null)
            {
                coverFinder.AdvanceQueue();

                if (!coverFinder.HasPoints)
                {
                    brain?.OnInvestigationFinished();
                }
            }
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public bool isDone;
        }
    }
}