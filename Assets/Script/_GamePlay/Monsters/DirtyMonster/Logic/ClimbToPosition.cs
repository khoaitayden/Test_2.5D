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
        if (Agent.Value == null) return Status.Failure;

        _agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_agent != null) _agent.enabled = false; // Detach from ground

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Transform trans = Agent.Value.transform;
        
        // 1. Move
        trans.position = Vector3.MoveTowards(trans.position, TargetPos.Value, Speed.Value * Time.deltaTime);

        // 2. Rotate
        Vector3 dirToTarget = (TargetPos.Value - trans.position).normalized;
        
        // If we are very close, don't jitter rotation
        if (dirToTarget != Vector3.zero && Vector3.Distance(trans.position, TargetPos.Value) > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            
            // Tweak: Rotate -90 on X so legs point to tree (assuming model is Y-up, Z-forward)
            // If your monster looks weird, remove this line or change to 90
            Quaternion surfaceRot = lookRot * Quaternion.Euler(0, 0, 0); 

            trans.rotation = Quaternion.Slerp(trans.rotation, surfaceRot, Time.deltaTime * 5f);
        }

        // 3. Finish
        if (Vector3.Distance(trans.position, TargetPos.Value) < 0.1f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Keep NavMesh disabled so it sticks to the tree
    }
}