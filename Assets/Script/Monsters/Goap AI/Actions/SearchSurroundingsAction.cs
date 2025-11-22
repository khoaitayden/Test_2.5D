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
        private MonsterConfig config;
        private MonsterBrain brain;
        private NavMeshAgent agentRaw; 

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // Cache components
            if (movement == null) movement = agent.GetComponent<MonsterMovement>();
            if (config == null) config = agent.GetComponent<MonsterConfig>();
            if (brain == null) brain = agent.GetComponent<MonsterBrain>();
            if (agentRaw == null) agentRaw = agent.GetComponent<NavMeshAgent>();

            data.investigationStartTime = Time.time;
            data.searchExhausted = false;
            data.pointsChecked = 0;
            
            // Generate points
            data.lookPoints = GenerateTacticalPoints(agent, data);
            data.totalPoints = data.lookPoints.Count;
            
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

            // Timeout check
            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                CompleteAction(data);
                return ActionRunState.Completed;
            }

            switch (data.state)
            {
                // ERROR WAS HERE: Now we correctly pass 'agent' to HandleMoving
                case SearchState.MovingToPoint: return HandleMoving(agent, data, context);
                case SearchState.ScanningAtPoint: return HandleScanning(agent, data, context);
            }

            return ActionRunState.Continue;
        }

        // ERROR FIXED: Added IMonoAgent agent parameter
        private IActionRunState HandleMoving(IMonoAgent agent, Data data, IActionContext context)
        {
            if (movement.IsStuck)
            {
                Debug.Log("[Search] Stuck moving to point. Skipping.");
                // ERROR FIXED: Used 'agent' parameter instead of 'context.Agent'
                if (!TryMoveToNextPoint(agent, data))
                {
                    CompleteAction(data);
                    return ActionRunState.Completed;
                }
            }

            if (movement.HasArrived)
            {
                // ERROR FIXED: Used 'agent' parameter instead of 'context.Agent'
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
                
                // Using Unified System
                if (movement.GoTo(nextPoint, MonsterMovement.SpeedState.Investigate))
                {
                    return true;
                }
            }
            
            return false;
        }

        private Queue<Vector3> GenerateTacticalPoints(IMonoAgent agent, Data data)
        {
            // Use the Optimized CoverFinder
            Vector3 searchCenter = data.Target.Position; 
            List<Vector3> foundPoints = CoverFinder.FindCoverPoints(searchCenter, config.investigateRadius, agent.Transform.position, config);
            
            foundPoints.Sort((a, b) => Vector3.SqrMagnitude(agent.Transform.position - a).CompareTo(Vector3.SqrMagnitude(agent.Transform.position - b)));
            
            int count = Mathf.Min(config.investigationPoints, foundPoints.Count);
            Queue<Vector3> queue = new Queue<Vector3>();
            for(int i = 0; i < count; i++) queue.Enqueue(foundPoints[i]);
            return queue;
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