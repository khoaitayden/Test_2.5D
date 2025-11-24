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
            data.isDone = false;
            
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

            // 2. No points logic
            if (data.totalPoints == 0)
            {
                Debug.LogWarning("[Search] No tactical points found. Investigation Finished immediately.");
                brain?.OnInvestigationFinished();
                data.isDone = true;
                return; 
            }

            // 3. Start first move
            if (!TryMoveToNextPoint(agent, data))
            {
                brain?.OnInvestigationFinished();
                data.isDone = true;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.isDone) return ActionRunState.Completed;

            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                return ActionRunState.Completed; 
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
            // Ensure brain knows we are done
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

            // FIX IS HERE: Use HasReached() with the specific point we are going to
            if (movement.HasReached(data.currentTargetPoint))
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
                if (!TryMoveToNextPoint(agent, data))
                {
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
            data.rotationAngle = Random.Range(90f, 180f); 
            data.rotatedAmount = 0f;
        }

        private bool TryMoveToNextPoint(IMonoAgent agent, Data data)
        {
            while (data.lookPoints.Count > 0)
            {
                Vector3 nextPoint = data.lookPoints.Dequeue();
                
                // Save current target so we can check arrival later
                data.currentTargetPoint = nextPoint;
                data.state = SearchState.MovingToPoint;
                
                if (movement.GoTo(nextPoint, MonsterMovement.SpeedState.Investigate))
                {
                    return true;
                }
            }
            return false;
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public Queue<Vector3> lookPoints;
            public Vector3 currentTargetPoint; // Added to track where we are going
            public bool isDone; 
            public int pointsChecked;
            public int totalPoints;
            public SearchState state;
            public float rotationSpeed;
            public float rotationAngle;
            public float rotatedAmount;
        }
    }
}