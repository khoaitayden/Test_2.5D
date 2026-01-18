using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Climb To Position", story: "[Agent] climbs to [TargetPos] at [Speed]", category: "Movement", id:"climbtopos")]
public partial class ClimbToPosition : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> TargetPos;
    [SerializeReference] public BlackboardVariable<float> Speed;

    private NavMeshAgent _agent;

    protected override Status OnStart()
    {
        Debug.Log("Climbing");
        if (Agent.Value == null) return Status.Failure;

        _agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_agent != null) _agent.enabled = false; // Detach from ground

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Transform trans = Agent.Value.transform;
        
        trans.position = Vector3.MoveTowards(trans.position, TargetPos.Value, Speed.Value * Time.deltaTime);

        
        Vector3 dirToTarget = (TargetPos.Value - trans.position).normalized;
        if (dirToTarget != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            Quaternion climbRot = lookRot * Quaternion.Euler(0, 0, 0); 
            
            trans.rotation = Quaternion.Slerp(trans.rotation, climbRot, Time.deltaTime * 5f);
        }

        if (Vector3.Distance(trans.position, TargetPos.Value) < 0.2f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}