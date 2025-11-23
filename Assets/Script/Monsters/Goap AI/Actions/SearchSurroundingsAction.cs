using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities; 
using System.Collections.Generic;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class SearchSurroundingsAction : GoapActionBase<SearchSurroundingsAction.Data>
    {
        public enum SearchState { MovingToPoint, ScanningAtPoint }

        private MonsterMovement movement; 
        private CoverFinder coverFinder;
        private MonsterConfig config;
        private MonsterBrain brain;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            if (coverFinder == null) coverFinder = agent.GetComponent<CoverFinder>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (brain == null) brain = agent.GetComponent<MonsterBrain>();

            data.investigationStartTime = Time.time;
            data.pointsChecked = 0;
            data.totalPoints = 0;
            
            // 1. Get Points
            if (coverFinder != null && data.Target != null)
            {
                var pointsList = coverFinder.GetCoverPointsAround(data.Target.Position);
                data.lookPoints = new Queue<Vector3>(pointsList);
                data.totalPoints = pointsList.Count;
                
                Debug.Log($"[Search] Found {data.totalPoints} points.");
            }
            else
            {
                data.lookPoints = new Queue<Vector3>();
            }

            // 2. LOGIC FIX: What if there are NO points?
            if (data.totalPoints == 0)
            {
                Debug.LogWarning("[Search] No tactical points found. Investigation Finished immediately.");
                // CRITICAL: We don't just complete the action, we tell the Brain "We are done looking".
                // This clears the 'IsInvestigating' state so Patrol can take over.
                brain?.OnInvestigationFinished();
                
                // End Action
                data.isDone = true;
                return; 
            }

            // 3. Start first move
            if (!TryMoveToNextPoint(agent, data))
            {
                // If points existed but were unreachable
                brain?.OnInvestigationFinished();
                data.isDone = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // If marked done in Start(), exit now
            if (data.isDone) return ActionRunState.Completed;

            // Timeout Check
            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                Debug.Log("[Search] Timed out.");
                return ActionRunState.Completed; // End will handle cleanup
            }

            switch (data.state)
            {
                case SearchState.MovingToPoint: 
                    return HandleMoving(agent, data);
                case SearchState.ScanningAtPoint: 
                    return HandleScanning(agent, data, context);
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            // ALWAYS tell the brain we are done when this action exits.
            // This ensures the WorldState 'IsInvestigating' flips to 0.
            Debug.Log("[Search] Action Ending -> Telling Brain Investigation is over.");
            brain?.OnInvestigationFinished();
        }

        private IActionRunState HandleMoving(IMonoAgent agent, Data data)
        {
            if (movement.IsStuck)
            {
                // If stuck, give up on this point, try next
                if (!TryMoveToNextPoint(agent, data))
                {
                    return ActionRunState.Completed;
                }
            }

            if (movement.HasArrived)
            {
                StartScanning(agent, data);
            }
            return ActionRunState.Continue;
        }

        private IActionRunState HandleScanning(IMonoAgent agent, Data data, IActionContext context)
        {
            float rotateStep = data.rotationSpeed * context.DeltaTime;
            agent.Transform.Rotate(0f, rotateStep, 0f);
            data.rotatedAmount += Mathf.Abs(rotateStep);

            if (data.rotatedAmount >= data.rotationAngle)
            {
                data.pointsChecked++;
                // Check if more points exist
                if (!TryMoveToNextPoint(agent, data))
                {
                    // No more points? We are totally done.
                    return ActionRunState.Completed;
                }
            }
            return ActionRunState.Continue;
        }

        private void StartScanning(IMonoAgent agent, Data data)
        {
            data.state = SearchState.ScanningAtPoint;
            movement.Stop();
            data.rotationSpeed = Random.Range(90f, 120f);
            data.rotationAngle = Random.Range(90f, 180f); // Spin a bit more
            data.rotatedAmount = 0f;
        }

        private bool TryMoveToNextPoint(IMonoAgent agent, Data data)
        {
            while (data.lookPoints.Count > 0)
            {
                Vector3 nextPoint = data.lookPoints.Dequeue();
                data.state = SearchState.MovingToPoint;
                
                if (movement.GoTo(nextPoint, MonsterMovement.SpeedState.Investigate))
                {
                    return true;
                }
            }
            // If queue is empty or all failed:
            return false;
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public Queue<Vector3> lookPoints;
            public bool isDone; // Helper flag
            public int pointsChecked;
            public int totalPoints;
            public SearchState state;
            public float rotationSpeed;
            public float rotationAngle;
            public float rotatedAmount;
        }
    }
}