using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Fly To Target", story: "[Agent] flies to [Target] at [Speed]", category: "Movement", id: "9823489234")] // Random ID is fine
public partial class FlyToTarget : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<float> StopDistance = new BlackboardVariable<float>(1.0f);

    private NavMeshAgent _navAgent;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
        {
            return Status.Failure;
        }

        // Disable NavMeshAgent so we can leave the ground
        _navAgent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_navAgent != null)
        {
            _navAgent.enabled = false;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null) return Status.Failure;

        // Move directly toward target (Flying logic)
        Transform agentTransform = Agent.Value.transform;
        Vector3 targetPos = Target.Value.position;

        // Move
        agentTransform.position = Vector3.MoveTowards(
            agentTransform.position, 
            targetPos, 
            Speed.Value * Time.deltaTime
        );

        // Rotate to look at target
        Vector3 direction = (targetPos - agentTransform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, lookRot, Time.deltaTime * 5f);
        }

        // Check if arrived
        if (Vector3.Distance(agentTransform.position, targetPos) <= StopDistance.Value)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_navAgent != null)
        {
            _navAgent.enabled = true; 
        }
    }
}