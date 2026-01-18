using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Fly Back To NavMesh", story: "[Agent] flies back to NavMesh within [Radius] at [Speed]", category: "Movement", id: "crawland")]
public partial class CrawBackToNavMesh : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<float> Speed;

    private NavMeshAgent _agent;
    private Vector3 _targetLandPosition;
    private bool _foundLandingSpot;

    protected override Status OnStart()
    {
        if (Agent.Value == null) return Status.Failure;

        _agent = Agent.Value.GetComponent<NavMeshAgent>();
        
        // Ensure Agent is disabled so we can move Transform manually
        if (_agent != null) _agent.enabled = false;

        _foundLandingSpot = FindValidLandingSpot();

        if (!_foundLandingSpot)
        {
            Debug.LogWarning("Could not find a valid NavMesh landing spot.");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_foundLandingSpot) return Status.Failure;

        Transform trans = Agent.Value.transform;

        // 1. Move towards the ground target
        trans.position = Vector3.MoveTowards(trans.position, _targetLandPosition, Speed.Value * Time.deltaTime);

        // 2. Rotate to face the landing spot AND level out (Upright)
        // This fixes the rotation from "Climbing Mode" (-90 pitch) back to "Walking Mode" (0 pitch)
        Vector3 dirToTarget = (_targetLandPosition - trans.position).normalized;
        
        if (dirToTarget != Vector3.zero)
        {
            // We force Vector3.up as the Upwards direction to make the monster upright again
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
            trans.rotation = Quaternion.Slerp(trans.rotation, lookRot, Time.deltaTime * 5f);
        }

        // 3. Check Arrival
        if (Vector3.Distance(trans.position, _targetLandPosition) < 0.2f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // CRITICAL: Re-enable the NavMeshAgent so the next "Navigate" node works!
        if (_agent != null)
        {
            _agent.enabled = true;
            // Sometimes enabling the agent snaps the Y position slightly, 
            // ensuring we are glued to the mesh.
        }
    }

    private bool FindValidLandingSpot()
    {
        Vector3 origin = Agent.Value.transform.position;

        // Try 10 times to find a valid spot
        for (int i = 0; i < 10; i++)
        {
            // Pick random point in circle (X, Z)
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Radius.Value;
            Vector3 searchPos = origin + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Raycast DOWN from the monster's current height to find the floor
            if (Physics.Raycast(searchPos, Vector3.down, out RaycastHit hit, 50f))
            {
                // Check if this hit point is on the NavMesh
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