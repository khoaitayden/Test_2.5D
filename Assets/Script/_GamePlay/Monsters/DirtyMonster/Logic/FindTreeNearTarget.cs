using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find Tree Near Target", story: "Find tree near [Target] within [Radius] on [Layer]", category: "Variable", id: "findtree")]
public partial class FindTreeNearTarget : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<float> treeClimbOffSet; 

    [SerializeReference] public BlackboardVariable<Vector2> minMaxHeight;
    private LayerMask TreeLayer;
    
    [SerializeReference] public BlackboardVariable<Vector3> FoundBasePosition;
    [SerializeReference] public BlackboardVariable<Vector3> FoundClimbPosition;

    protected override Status OnStart()
    {
        TreeLayer = LayerMask.GetMask("Tree");
        
        if (Target.Value == null) return Status.Failure;

        Collider[] hits = Physics.OverlapSphere(Target.Value.transform.position, Radius.Value, TreeLayer);
        
        if (hits.Length == 0) {
            Debug.LogWarning("Found no trees in range");
            return Status.Failure;
        }

        Collider randomTree = hits[UnityEngine.Random.Range(0, hits.Length)];
        Vector3 treeCenter = randomTree.bounds.center;

        Vector3 rawBasePos = new Vector3(treeCenter.x, randomTree.bounds.min.y, treeCenter.z);

        if (NavMesh.SamplePosition(rawBasePos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            FoundBasePosition.Value = hit.position;
        }
        else
        {
            FoundBasePosition.Value = rawBasePos;
        }

        float climbHeight = UnityEngine.Random.Range(minMaxHeight.Value.x, minMaxHeight.Value.y);
        float targetY = FoundBasePosition.Value.y + climbHeight;

        Vector3 approachPointAtHeight = new Vector3(FoundBasePosition.Value.x, targetY, FoundBasePosition.Value.z);

        Vector3 surfacePoint = randomTree.ClosestPoint(approachPointAtHeight);

        Vector3 centerAtHeight = new Vector3(treeCenter.x, targetY, treeCenter.z);
        Vector3 directionOut = (surfacePoint - centerAtHeight).normalized;

        FoundClimbPosition.Value = surfacePoint + (directionOut * treeClimbOffSet.Value);

        return Status.Success;
    }
}