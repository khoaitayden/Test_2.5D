using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
            data.searchExhausted = false;
            
            // USE THE COMPONENT
            // The logic of "How to find cover" is now completely hidden from this action.
            if (coverFinder != null && data.Target != null)
            {
                var pointsList = coverFinder.GetCoverPointsAround(data.Target.Position);
                data.lookPoints = new Queue<Vector3>(pointsList);
                
                // Debug log
                Debug.Log($"[Search] CoverFinder component returned {pointsList.Count} tactical spots.");
            }
            else
            {
                data.lookPoints = new Queue<Vector3>();
            }

            data.totalPoints = data.lookPoints.Count;
            data.pointsChecked = 0;

            if (data.totalPoints > 0)
            {
                TryMoveToNextPoint(agent, data);
            }
            else
            {
                CompleteAction(data);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.searchExhausted) return ActionRunState.Completed;

            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                CompleteAction(data);
                return ActionRunState.Completed;
            }

            switch (data.state)
            {
                case SearchState.MovingToPoint: return HandleMoving(agent, data);
                case SearchState.ScanningAtPoint: return HandleScanning(agent, data, context);
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleMoving(IMonoAgent agent, Data data)
        {
            if (movement.IsStuck)
            {
                if (!TryMoveToNextPoint(agent, data))
                {
                    CompleteAction(data);
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
                if (!TryMoveToNextPoint(agent, data))
                {
                    CompleteAction(data);
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
            data.rotationAngle = Random.Range(90f, 120f);
            data.rotatedAmount = 0f;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            brain?.OnInvestigationFinished();
        }

        private void CompleteAction(Data data)
        {
            if (!data.searchExhausted)
            {
                data.searchExhausted = true;
            }
        }

        private bool TryMoveToNextPoint(IMonoAgent agent, Data data)
        {
            while (data.lookPoints.Count > 0)
            {
                Vector3 nextPoint = data.lookPoints.Dequeue();
                data.state = SearchState.MovingToPoint;
                
                // Unified Movement Call
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
            public bool searchExhausted;
            public int pointsChecked;
            public int totalPoints;
            public SearchState state;
            public float rotationSpeed;
            public float rotationAngle;
            public float rotatedAmount;
        }
    }
}