using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Docs.GettingStarted.Behaviours
{
    public class MonsterMoveBehaviour : MonoBehaviour
    {
        private AgentBehaviour agent;
        private NavMeshAgent navMeshAgent;
        private ITarget currentTarget;
        private bool shouldMove;

        private void Awake()
        {
            this.agent = this.GetComponent<AgentBehaviour>();
            this.navMeshAgent = this.GetComponent<NavMeshAgent>();

            if (navMeshAgent == null)
            {
                Debug.LogError("MonsterMoveBehaviour requires a NavMeshAgent component!");
            }
        }

        private void OnEnable()
        {
            this.agent.Events.OnTargetInRange += this.OnTargetInRange;
            this.agent.Events.OnTargetChanged += this.OnTargetChanged;
            this.agent.Events.OnTargetNotInRange -= this.TargetNotInRange; // ⚠️ Fix: was -= in OnEnable!
            this.agent.Events.OnTargetNotInRange += this.TargetNotInRange;
            this.agent.Events.OnTargetLost += this.TargetLost;
        }

        private void OnDisable()
        {
            this.agent.Events.OnTargetInRange -= this.OnTargetInRange;
            this.agent.Events.OnTargetChanged -= this.OnTargetChanged;
            this.agent.Events.OnTargetNotInRange -= this.TargetNotInRange;
            this.agent.Events.OnTargetLost -= this.TargetLost;
        }

        private void TargetLost()
        {
            this.currentTarget = null;
            this.shouldMove = false;
            this.navMeshAgent.isStopped = true;
        }

        private void OnTargetInRange(ITarget target)
        {
            this.shouldMove = false;
            this.navMeshAgent.isStopped = true;
        }

        private void OnTargetChanged(ITarget target, bool inRange)
        {
            this.currentTarget = target;
            this.shouldMove = !inRange;
            if (this.shouldMove && target != null)
            {
                this.navMeshAgent.SetDestination(target.Position);
                this.navMeshAgent.isStopped = false;
            }
        }

        private void TargetNotInRange(ITarget target)
        {
            this.shouldMove = true;
            if (target != null)
            {
                this.navMeshAgent.SetDestination(target.Position);
                this.navMeshAgent.isStopped = false;
            }
        }

        // No Update needed for movement! NavMeshAgent handles it.
        // But you can keep it for debugging or pausing logic.
        private void Update()
        {
            if (this.agent.IsPaused)
            {
                this.navMeshAgent.isStopped = true;
            }
            else if (this.shouldMove && this.currentTarget != null)
            {
                // Optional: refresh destination if it drifts (usually not needed)
                // this.navMeshAgent.SetDestination(this.currentTarget.Position);
            }

            Debug.LogWarning("Path to target is invalid!");
        }

        private void OnDrawGizmos()
        {
            if (this.currentTarget == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.transform.position, this.currentTarget.Position);
        }
    }
}