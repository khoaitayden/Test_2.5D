using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Fly Back To NavMesh", story: "[Agent] flies back to NavMesh", category: "Movement", id: "flyland")]
public partial class FlyBackToNavMesh : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<float> Speed;

    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> IsFlying;

    private NavMeshAgent _agent;
    private Vector3 _targetLandPosition;
    private bool _foundLandingSpot;

    protected override Status OnStart()
    {
        if (Agent.Value == null) return Status.Failure;

        _agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_agent != null) _agent.enabled = false;

        _foundLandingSpot = FindValidLandingSpot();

        if (!_foundLandingSpot) return Status.Failure;

        // Start Animation
        if (Animator.Value != null) Animator.Value.SetBool(IsFlying.Value, true);
        Debug.Log("FlyingDown");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_foundLandingSpot) return Status.Failure;

        Transform trans = Agent.Value.transform;
        trans.position = Vector3.MoveTowards(trans.position, _targetLandPosition, Speed.Value * Time.deltaTime);

        Vector3 dirToTarget = (_targetLandPosition - trans.position).normalized;
        if (dirToTarget != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
            trans.rotation = Quaternion.Slerp(trans.rotation, lookRot, Time.deltaTime * 5f);
        }

        if (Vector3.Distance(trans.position, _targetLandPosition) < 0.2f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_agent != null) _agent.enabled = true;

        // Stop Animation
        if (Animator.Value != null) Animator.Value.SetBool(IsFlying.Value, false);
    }

    private bool FindValidLandingSpot()
    {
        Vector3 origin = Agent.Value.transform.position;
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Radius.Value;
            Vector3 searchPos = origin + new Vector3(randomCircle.x, 0, randomCircle.y);
            if (Physics.Raycast(searchPos, Vector3.down, out RaycastHit hit, 50f))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    _targetLandPosition = navHit.position;
                    return true;
                }
            }
        }
        return false;
    }
}