// FILE TO EDIT: SearchSurroundingsAction.cs

using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
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
        private StuckDetector stuckDetector = new StuckDetector();

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            // --- Standard Setup ---
            navMeshAgent ??= agent.GetComponent<NavMeshAgent>();
            config ??= agent.GetComponent<MonsterConfig>();
            stuckDetector.Reset();

            // --- Initialize Action ---
            data.investigationStartTime = Time.time;
            data.searchExhausted = false;
            data.state = SearchState.MovingToPoint;
            data.pointsChecked = 0; // Track how many we've actually visited
            
            Debug.Log("[Search] Starting tactical search for cover points...");
            MonsterSpeedController.SetSpeedMode(navMeshAgent, config, MonsterSpeedController.SpeedMode.InvestigateSearch);
            
            // Generate the entire list of points to check at the beginning.
            data.lookPoints = GenerateTacticalPoints(agent, data);
            data.totalPoints = data.lookPoints.Count;

            // Start moving to the first point.
            if (!MoveToNextLookPoint(agent, data))
            {
                // Edge case: No valid points were found, so complete the action immediately.
                Debug.LogWarning("[Search] No valid cover points found. Completing search early.");
                CompleteAction(data);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // If the action has already decided it's done, just wait for the framework to end it.
            if (data.searchExhausted)
            {
                return ActionRunState.Completed;
            }

            // --- Global Checks ---
            if (Time.time - data.investigationStartTime > config.maxInvestigationTime)
            {
                Debug.Log($"[Search] Giving up after timeout.");
                CompleteAction(data);
                return ActionRunState.Completed;
            }

            // Handle different states
            switch (data.state)
            {
                case SearchState.MovingToPoint:
                    return HandleMoving(agent, data, context);
                    
                case SearchState.ScanningAtPoint:
                    return HandleScanning(agent, data, context);
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleMoving(IMonoAgent agent, Data data, IActionContext context)
        {
            // Check if we are stuck
            if (stuckDetector.CheckStuck(agent.Transform.position, context.DeltaTime, config))
            {
                Debug.LogWarning($"[Search] STUCK while moving. Trying next point.");
                
                // Reset stuck detector before trying next point
                stuckDetector.Reset();
                
                if (!MoveToNextLookPoint(agent, data))
                {
                    CompleteAction(data);
                    return ActionRunState.Completed;
                }
                // Continue trying the new point
                return ActionRunState.Continue;
            }

            // Check if we have arrived at our current destination
            bool hasArrived = !navMeshAgent.pathPending && 
                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f;

            if (hasArrived)
            {
                Debug.Log($"[Search] Arrived at cover point #{data.pointsChecked + 1}/{data.totalPoints}. Beginning scan...");
                StartScanning(agent, data);
            }
            
            return ActionRunState.Continue;
        }

        private IActionRunState HandleScanning(IMonoAgent agent, Data data, IActionContext context)
        {
            // Rotate the monster to scan the area
            float rotateStep = data.rotationSpeed * context.DeltaTime * data.rotationDirection;
            agent.Transform.Rotate(0f, rotateStep, 0f);
            data.rotatedAmount += Mathf.Abs(rotateStep);

            // Check if we've completed the first sweep
            if (data.rotatedAmount >= data.rotationAngle)
            {
                if (data.rotationDirection == 1)
                {
                    // We've scanned right, now scan left
                    data.rotationDirection = -1;
                    data.rotatedAmount = 0f;
                    Debug.Log($"[Search] Scanning back...");
                }
                else
                {
                    // We've completed both sweeps at this point
                    data.pointsChecked++;
                    Debug.Log($"[Search] Point #{data.pointsChecked}/{data.totalPoints} scanned. Checking for more points...");
                    
                    if (!MoveToNextLookPoint(agent, data))
                    {
                        // No more points to check
                        Debug.Log($"[Search] All {data.pointsChecked} cover points checked!");
                        CompleteAction(data);
                        return ActionRunState.Completed;
                    }
                }
            }
            
            return ActionRunState.Continue;
        }

        private void StartScanning(IMonoAgent agent, Data data)
        {
            data.state = SearchState.ScanningAtPoint;
            
            // Stop moving while we scan
            navMeshAgent.isStopped = true;
            
            // Configure rotation behavior
            data.rotationSpeed = Random.Range(60f, 90f);      // Degrees per second
            data.rotationAngle = Random.Range(120f, 180f);   // Total angle to scan
            data.rotationDirection = 1;                       // Start by rotating right
            data.rotatedAmount = 0f;
            
            stuckDetector.Reset();
            
            Debug.Log($"[Search] Scanning {data.rotationAngle:F0}° at {data.rotationSpeed:F0}°/s");
        }

        public override void End(IMonoAgent agent, Data data)
        {
            Debug.Log($"[Search] End() called. Checked {data.pointsChecked}/{data.totalPoints} points. Exhausted: {data.searchExhausted}");
            
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.ResetPath();
            }
            stuckDetector.Reset();

            var provider = agent.GetComponent<GoapActionProvider>();
            provider?.WorldData.SetState(new IsAtSuspiciousLocation(), 0);

            var brain = agent.GetComponent<MonsterBrain>();
            brain?.OnInvestigationComplete();
        }

        private void CompleteAction(Data data)
        {
            Debug.Log("[Search] ========== SEARCH COMPLETE ==========");
            data.searchExhausted = true;
        }

        private bool MoveToNextLookPoint(IMonoAgent agent, Data data)
        {
            if (data.lookPoints.Count == 0)
            {
                Debug.Log("[Search] No more points in queue.");
                return false;
            }
            
            Vector3 nextPoint = data.lookPoints.Dequeue();
            
            // Switch back to moving state BEFORE setting destination
            data.state = SearchState.MovingToPoint;
            
            // Ensure agent is ready to move
            navMeshAgent.isStopped = false;
            bool destinationSet = navMeshAgent.SetDestination(nextPoint);
            
            if (!destinationSet)
            {
                Debug.LogWarning($"[Search] Failed to set destination to {nextPoint}!");
                return false;
            }
            
            stuckDetector.StartTracking(agent.Transform.position);
            
            Debug.Log($"[Search] Moving to point #{data.pointsChecked + 1}/{data.totalPoints} ({data.lookPoints.Count} remaining in queue)");
            return true;
        }

        private Queue<Vector3> GenerateTacticalPoints(IMonoAgent agent, Data data)
        {
            // Use our CoverFinder utility to find points relative to the player's last known location.
            Vector3 searchCenter = data.Target.Position; 
            
            // Find a list of potential cover points using the new simplified config values.
            List<Vector3> foundPoints = CoverFinder.FindCoverPoints(searchCenter, config.investigateRadius, agent.Transform.position, config);

            // Prioritize the points by checking the closest ones first.
            foundPoints.Sort((a, b) => 
                Vector3.Distance(agent.Transform.position, a).CompareTo(Vector3.Distance(agent.Transform.position, b))
            );

            // Take only the best 'investigationPoints' to keep the search focused.
            List<Vector3> finalPoints = new List<Vector3>();
            for(int i = 0; i < Mathf.Min(config.investigationPoints, foundPoints.Count); i++)
            {
                finalPoints.Add(foundPoints[i]);
            }

            Debug.Log($"[Search] Generated {finalPoints.Count}/{config.investigationPoints} tactical points to check.");
            
            // Convert the final list into the Queue the rest of the action expects.
            return new Queue<Vector3>(finalPoints);
        }
        
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float investigationStartTime;
            public Queue<Vector3> lookPoints;
            public bool searchExhausted;
            public int pointsChecked;  // How many points we've actually scanned
            public int totalPoints;    // Total points to check
            
            // State machine
            public SearchState state;
            
            // Rotation/scanning data
            public float rotationSpeed;
            public float rotationAngle;
            public float rotatedAmount;
            public int rotationDirection;  // 1 for right, -1 for left
        }
    }
}