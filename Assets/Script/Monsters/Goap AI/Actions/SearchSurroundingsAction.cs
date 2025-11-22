using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Docs.GettingStarted.Behaviours; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class SearchSurroundingsAction : GoapActionBase<SearchSurroundingsAction.Data>
    {
        public enum SearchState { MovingToPoint, ScanningAtPoint }

        private NavMeshAgent navMeshAgent;
        private MonsterConfig config;
        private MonsterBrain brain;
        private StuckDetector stuckDetector = new StuckDetector();
        private MonsterMoveBehaviour moveBehaviour;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            navMeshAgent = agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();
            stuckDetector.Reset();
            brain ??= agent.GetComponent<MonsterBrain>();
            
            moveBehaviour ??= agent.GetComponent<MonsterMoveBehaviour>();
            if (moveBehaviour != null)
            {
                moveBehaviour.Stop(); 
                moveBehaviour.enabled = false;
            }

            data.investigationStartTime = Time.time;
            data.searchExhausted = false;
            data.pointsChecked = 0;
            
            // This is now much faster because FindCoverPoints doesn't pathfind
            data.lookPoints = GenerateTacticalPoints(agent, data);
            data.totalPoints = data.lookPoints.Count;
            
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateSearch);
            
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
                case SearchState.MovingToPoint: return HandleMoving(agent, data, context);
                case SearchState.ScanningAtPoint: return HandleScanning(agent, data, context);
            }
            return ActionRunState.Continue;
        }

        private IActionRunState HandleMoving(IMonoAgent agent, Data data, IActionContext context)
        {
            // Optimization: Perform squared distance check manually before accessing navMeshAgent
            // accessing native navMesh properties repeatedly is slightly more expensive than math
            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                stuckDetector.Reset();
                // If stuck, force skip to next
                if (!TryMoveToNextPoint(agent, data)) 
                {
                    CompleteAction(data);
                    return ActionRunState.Completed;
                }
            }

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                StartScanning(agent, data);
            }
            return ActionRunState.Continue;
        }

        private IActionRunState HandleScanning(IMonoAgent agent, Data data, IActionContext context)
        {
            // Optimization: Rotate check logic is fine, light on math
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
            // Check navmesh before stopping to avoid errors
            if(navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = true;
            
            data.rotationSpeed = Random.Range(30f, 90f);
            data.rotationAngle = Random.Range(30f, 90f);
            data.rotatedAmount = 0f;
            stuckDetector.Reset();
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) 
            {
                navMeshAgent.isStopped = true; 
                navMeshAgent.ResetPath();
            }
            stuckDetector.Reset();
            
            if (moveBehaviour != null) moveBehaviour.enabled = true;
            brain?.OnInvestigationFinished();
        }

        private void CompleteAction(Data data)
        {
            if (!data.searchExhausted)
            {
                data.searchExhausted = true;
            }
        }

        // RECURSIVE VALIDATION to skip unreachable points instantly
        private bool TryMoveToNextPoint(IMonoAgent agent, Data data)
        {
            while (data.lookPoints.Count > 0)
            {
                Vector3 nextPoint = data.lookPoints.Dequeue();
                
                // Use NavMeshPath to verify connectivity asynchronously logic 
                // Or rely on SetDestination returning false.
                
                data.state = SearchState.MovingToPoint;
                if(navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;

                // SetDestination is the 'CalculatePath' check here.
                // It's generally faster than a full separate calculation, 
                // but if it fails (returns false), we immediately try the next point loop.
                if (navMeshAgent.SetDestination(nextPoint))
                {
                    stuckDetector.StartTracking(agent.Transform.position);
                    
                    // Optional: Visual debugging
                    Debug.DrawLine(agent.Transform.position, nextPoint, Color.cyan, 2f);
                    return true; // Success, we are moving
                }
                
                // If we get here, SetDestination failed (point unreachable).
                // loop continues to next point immediately without waiting for next frame.
            }
            
            return false; // No valid points left
        }

        private Queue<Vector3> GenerateTacticalPoints(IMonoAgent agent, Data data)
        {
            // CoverFinder is now lightweight
            List<Vector3> foundPoints = CoverFinder.FindCoverPoints(data.Target.Position, config.investigateRadius, agent.Transform.position, config);
            
            // Simple distance sort is fine
            foundPoints.Sort((a, b) => Vector3.SqrMagnitude(agent.Transform.position - a).CompareTo(Vector3.SqrMagnitude(agent.Transform.position - b)));
            
            int count = Mathf.Min(config.investigationPoints, foundPoints.Count);
            var q = new Queue<Vector3>(count);
            for(int i=0; i<count; i++) q.Enqueue(foundPoints[i]);
            
            return q;
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