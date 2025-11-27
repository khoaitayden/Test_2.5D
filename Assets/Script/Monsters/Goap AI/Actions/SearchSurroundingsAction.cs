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
            data.lookPoints = new Queue<Vector3>();
            data.isDone = false;
            
            if (coverFinder != null && data.Target != null)
            {
                var list = coverFinder.GetCoverPointsAround(data.Target.Position, agent.Transform.position);
                foreach(var p in list) data.lookPoints.Enqueue(p);
            }

            // Move to first point
            if (!NextPoint(data))
            {
                brain?.OnInvestigationFinished();
                data.isDone = true;
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

            // FIX: Use the master check function
            if (movement.HasArrivedOrStuck())
            {
                // Arrived or stuck -> Try next point
                if (!NextPoint(data))
                {
                    return ActionRunState.Completed;
                }
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            brain?.OnInvestigationFinished();
        }

        private bool NextPoint(Data data)
        {
            if (data.lookPoints.Count == 0) return false;

            Vector3 dest = data.lookPoints.Dequeue();
            movement.GoTo(dest, MonsterMovement.SpeedState.Investigate);
            return true;
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public Queue<Vector3> lookPoints;
            public bool isDone; 
        }
    }
}