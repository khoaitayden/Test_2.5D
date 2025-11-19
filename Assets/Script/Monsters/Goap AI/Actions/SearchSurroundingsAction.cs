// FILE TO EDIT: SearchSurroundingsAction.cs

using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Docs.GettingStarted.Behaviours; // Make sure this namespace is correct
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Agent.Runtime;

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
            if (navMeshAgent == null)
            {
                Debug.LogError("[Search] NavMeshAgent is NULL. Cannot start action.");
                CompleteAction(data);
                return; // Abort the start method.
            }
            config ??= agent.GetComponent<MonsterConfig>();
            stuckDetector.Reset();
            brain ??= agent.GetComponent<MonsterBrain>();
            moveBehaviour ??= agent.GetComponent<MonsterMoveBehaviour>();
            if (moveBehaviour != null)
            {
                moveBehaviour.Stop(); 
                moveBehaviour.enabled = false;
                Debug.Log("[Search] Disabled MonsterMoveBehaviour for manual control.");
            }

            data.investigationStartTime = Time.time;
            data.searchExhausted = false;
            data.pointsChecked = 0;
            
            data.lookPoints = GenerateTacticalPoints(agent, data);
            data.totalPoints = data.lookPoints.Count;
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateSearch);

            Debug.Log($"[Search] Starting search. Found {data.totalPoints} tactical points.");
            
            if (data.totalPoints > 0)
            {
                Debug.Log("Moving");
                MoveToNextLookPoint(agent, data);
            }
            else
            {
                Debug.LogWarning("[Search] No tactical points found. Completing search action.");
                CompleteAction(data);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (navMeshAgent == null)
            {
                Debug.LogError("[Search] NavMeshAgent became NULL during Perform. Aborting.");
                CompleteAction(data);
                return ActionRunState.Completed;
            }
            if (data.searchExhausted) return ActionRunState.Completed;


            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                Debug.Log("[Search] Investigation time out");
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
            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                stuckDetector.Reset();
                if (!MoveToNextLookPoint(agent, data))
                {
                    Debug.Log("[Search] Monster got stuck at last point abort mission");
                    CompleteAction(data);
                    return ActionRunState.Completed;
                }
                return ActionRunState.Continue;
            }
            bool hasArrived = !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance;
            if (hasArrived)
            {
                Debug.Log("Arrived");
                StartScanning(agent, data);
            }
            return ActionRunState.Continue;
            
        }

       private IActionRunState HandleScanning(IMonoAgent agent, Data data, IActionContext context)
        {
            float rotateStep = data.rotationSpeed * context.DeltaTime * 1;
            agent.Transform.Rotate(0f, rotateStep, 0f);
            data.rotatedAmount += Mathf.Abs(rotateStep);

            if (data.rotatedAmount >= data.rotationAngle)
            {
                    data.pointsChecked++;
                    Debug.Log($"[Search] Point #{data.pointsChecked}/{data.totalPoints} scanned.");

                    // After scanning, try to move to the NEXT point.
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        // If MoveToNextLookPoint returns false, it means the queue is empty. The search is over.
                        Debug.Log("[Search] All points checked.");
                        CompleteAction(data);
                        return ActionRunState.Completed;
                    }
            }
            return ActionRunState.Continue;
        }

        private void StartScanning(IMonoAgent agent, Data data)
        {
            Debug.Log("Init scanning");
            data.state = SearchState.ScanningAtPoint;
            navMeshAgent.isStopped = true;
            data.rotationSpeed = Random.Range(90f, 120f);
            data.rotationAngle = Random.Range(90f, 120f);
            data.rotatedAmount = 0f;
            stuckDetector.Reset();
        }

        public override void End(IMonoAgent agent, Data data)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
            stuckDetector.Reset();
            
            if (moveBehaviour != null)
            {
                moveBehaviour.enabled = true;
                Debug.Log("[Search] Re-enabled MonsterMoveBehaviour.");
            }

            // Report to the brain that the entire investigation process is over.
            brain?.OnInvestigationFinished();
        }

        private void CompleteAction(Data data)
        {
            if (!data.searchExhausted)
            {
                Debug.Log("[Search] ========== SEARCH COMPLETE ==========");
                data.searchExhausted = true;
            }
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
            {
                return false;
            }
            
            Vector3 nextPoint = data.lookPoints.Dequeue();
            data.state = SearchState.MovingToPoint;
            navMeshAgent.isStopped = false;

            if (!navMeshAgent.SetDestination(nextPoint))
            {
                // If setting destination fails, this also signals failure to the caller.
                return false;
            }

            stuckDetector.StartTracking(agent.Transform.position);
            return true;
        }

        private Queue<Vector3> GenerateTacticalPoints(IMonoAgent agent, Data data)
        {
            Vector3 searchCenter = data.Target.Position; 
            List<Vector3> foundPoints = CoverFinder.FindCoverPoints(searchCenter, config.investigateRadius, agent.Transform.position, config);
            foundPoints.Sort((a, b) => Vector3.Distance(agent.Transform.position, a).CompareTo(Vector3.Distance(agent.Transform.position, b)));
            List<Vector3> finalPoints = new List<Vector3>();
            for(int i = 0; i < Mathf.Min(config.investigationPoints, foundPoints.Count); i++)
            {
                finalPoints.Add(foundPoints[i]);
            }
            return new Queue<Vector3>(finalPoints);
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