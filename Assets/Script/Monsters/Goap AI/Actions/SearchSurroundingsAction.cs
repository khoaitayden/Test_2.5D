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
            // Cache Refs
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            if (coverFinder == null) coverFinder = agent.GetComponent<CoverFinder>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (brain == null) brain = agent.GetComponent<MonsterBrain>();

            // Init
            data.investigationStartTime = Time.time;
            data.lookPoints = new Queue<Vector3>();
            data.currentPointStartTime = 0f; // Reset
            
            // 1. Get Points
            if (coverFinder != null && data.Target != null)
            {
                var list = coverFinder.GetCoverPointsAround(data.Target.Position, agent.Transform.position);
                foreach(var p in list) data.lookPoints.Enqueue(p);
                Debug.Log($"[SearchAction] Starting. Points found: {data.lookPoints.Count}");
            }

            // 2. Handle No Points
            if (data.lookPoints.Count == 0)
            {
                Debug.LogWarning("[SearchAction] No points. Done.");
                brain?.OnInvestigationFinished();
                data.isDone = true;
                return; 
            }

            // 3. Start Move
            data.isDone = false;
            if(!MoveToNext(data))
            {
                brain?.OnInvestigationFinished();
                data.isDone = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // DEBUG LINE: Verify loop integrity
            // Debug.Log("[SearchAction] Perform Running...");

            if (data.isDone) return ActionRunState.Completed;

            // 1. Global Timeout
            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                Debug.Log("[SearchAction] Global Timeout.");
                return ActionRunState.Completed; 
            }

            // 2. Point Timeout Check
            // Note: 'currentPointStartTime' set in MoveToNext
            float pointDuration = Time.time - data.currentPointStartTime;
            
            if (pointDuration > 5.0f)
            {
                Debug.Log($"[SearchAction] Point TIMEOUT ({pointDuration:F1}s). Forcing Next.");
                if (!MoveToNext(data)) return ActionRunState.Completed;
                return ActionRunState.Continue; // Skip arrival check this frame
            }

            // 3. Check Arrival using Component
            if (movement.HasReachedDestination())
            {
                Debug.Log("[SearchAction] HasReached = TRUE. Moving Next.");
                if (!MoveToNext(data)) return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            Debug.Log("[SearchAction] End.");
            movement.Stop();
            brain?.OnInvestigationFinished();
        }

        private bool MoveToNext(Data data)
        {
            while (data.lookPoints.Count > 0)
            {
                Vector3 p = data.lookPoints.Dequeue();
                
                // Set Timer BEFORE action
                data.currentPointStartTime = Time.time;
                Debug.Log($"[SearchAction] Moving to next point... (Rem: {data.lookPoints.Count})");

                if (movement.GoTo(p, MonsterMovement.SpeedState.Investigate))
                {
                    return true;
                }
                else
                {
                     Debug.LogWarning("[SearchAction] GoTo Failed (Unreachable?). trying next.");
                }
            }
            
            Debug.Log("[SearchAction] No more points.");
            return false;
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public float currentPointStartTime;
            public Queue<Vector3> lookPoints;
            public bool isDone; 
        }
    }
}