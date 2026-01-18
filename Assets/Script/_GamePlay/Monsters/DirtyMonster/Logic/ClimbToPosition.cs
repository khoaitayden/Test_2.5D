using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Climb To Position", story: "[Agent] climbs to [TargetPos] at [Speed] (Anim: [AnimBool])", category: "Movement", id:"climbtopos")]
public partial class ClimbToPosition : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> TargetPos;
    [SerializeReference] public BlackboardVariable<float> Speed;
    
    // NEW: Animation Support
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> AnimBool = new BlackboardVariable<string>("IsClimbing");

    private NavMeshAgent _agent;

    protected override Status OnStart()
    {
        if (Agent.Value == null) return Status.Failure;

        _agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_agent != null) _agent.enabled = false; 

        // Start Animation
        if (Animator.Value != null) Animator.Value.SetBool(AnimBool.Value, true);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Transform trans = Agent.Value.transform;

        trans.position = Vector3.MoveTowards(trans.position, TargetPos.Value, Speed.Value * Time.deltaTime);

        Vector3 dirToTarget = (TargetPos.Value - trans.position).normalized;
        if (dirToTarget != Vector3.zero && Vector3.Distance(trans.position, TargetPos.Value) > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            Quaternion surfaceRot = lookRot * Quaternion.Euler(0, 0, 0); 
            trans.rotation = Quaternion.Slerp(trans.rotation, surfaceRot, Time.deltaTime * 5f);
        }

        if (Vector3.Distance(trans.position, TargetPos.Value) < 0.1f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Animator.Value != null) Animator.Value.SetBool(AnimBool.Value, false);
    }
}