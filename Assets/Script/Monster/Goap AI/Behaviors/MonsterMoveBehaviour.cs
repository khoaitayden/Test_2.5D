// // FILE TO EDIT: MonsterMoveBehaviour.cs
// using CrashKonijn.Agent.Core;
// using CrashKonijn.Agent.Runtime;
// using UnityEngine;
// using UnityEngine.AI;

// // This namespace was different in your provided file, ensure it's correct for your project
// namespace CrashKonijn.Docs.GettingStarted.Behaviours 
// {
//     public class MonsterMoveBehaviour : MonoBehaviour
//     {
//         private AgentBehaviour agent;
//         private NavMeshAgent navMeshAgent;
//         private ITarget currentTarget;
//         private bool shouldMove;
        
//         // You can add a small delay to avoid setting the destination every single frame
//         private float pathUpdateDelay = 0.1f; 
//         private float pathUpdateTimer;

//         private void Awake()
//         {
//             this.agent = this.GetComponent<AgentBehaviour>();
//             this.navMeshAgent = this.GetComponent<NavMeshAgent>();

//             if (navMeshAgent == null)
//                 Debug.LogError("MonsterMoveBehaviour requires a NavMeshAgent component!");
//         }

//         // OnEnable and OnDisable remain unchanged...
//         private void OnEnable()
//         {
//             this.agent.Events.OnTargetInRange += this.OnTargetInRange;
//             this.agent.Events.OnTargetChanged += this.OnTargetChanged;
//             this.agent.Events.OnTargetNotInRange += this.TargetNotInRange;
//             this.agent.Events.OnTargetLost += this.TargetLost;
//         }

//         private void OnDisable()
//         {
//             this.agent.Events.OnTargetInRange -= this.OnTargetInRange;
//             this.agent.Events.OnTargetChanged -= this.OnTargetChanged;
//             this.agent.Events.OnTargetNotInRange -= this.TargetNotInRange;
//             this.agent.Events.OnTargetLost -= this.TargetLost;
//         }

//         private void TargetLost()
//         {
//             this.currentTarget = null;
//             this.shouldMove = false;
//             if (this.navMeshAgent.isOnNavMesh)
//                 this.navMeshAgent.isStopped = true;
//         }

//         private void OnTargetInRange(ITarget target)
//         {
//             this.shouldMove = false;
//             if (this.navMeshAgent.isOnNavMesh)
//                 this.navMeshAgent.isStopped = true;
//         }

//         private void OnTargetChanged(ITarget target, bool inRange)
//         {
//             this.currentTarget = target;
//             this.shouldMove = !inRange;
//             // Set initial destination
//             this.UpdatePath();
//         }

//         private void TargetNotInRange(ITarget target)
//         {
//             this.shouldMove = true;
//              // Set initial destination
//             this.UpdatePath();
//         }
        
//         // This is where the continuous chase logic lives
//         private void Update()
//         {
//             if (this.agent.IsPaused)
//             {
//                 if(this.navMeshAgent.isOnNavMesh)
//                     this.navMeshAgent.isStopped = true;
//                 return; // Don't do anything else if paused
//             }

//             // Timer to prevent updating path every single frame (better performance)
//             pathUpdateTimer -= Time.deltaTime;

//             if (this.shouldMove && this.currentTarget != null)
//             {
//                 if(pathUpdateTimer <= 0f)
//                 {
//                     this.UpdatePath();
//                 }
//             }
//         }
        
//         private void UpdatePath()
//         {
//             if (currentTarget == null || !this.navMeshAgent.isOnNavMesh)
//                 return;
                
//             this.navMeshAgent.SetDestination(this.currentTarget.Position);
//             this.navMeshAgent.isStopped = false;
            
//             // Reset timer
//             pathUpdateTimer = pathUpdateDelay;
//         }

//         private void OnDrawGizmos()
//         {
//             if (this.currentTarget == null) return;
//             Gizmos.color = Color.red;
//             Gizmos.DrawLine(this.transform.position, this.currentTarget.Position);
//         }
//     }
// }