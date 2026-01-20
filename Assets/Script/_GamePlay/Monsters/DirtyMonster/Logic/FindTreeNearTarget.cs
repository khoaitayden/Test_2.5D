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
    [SerializeReference] public BlackboardVariable<TransformAnchorSO> Target;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<float> treeClimbOffSet; 

    [SerializeReference] public BlackboardVariable<Vector2> minMaxHeight;
    private LayerMask TreeLayer;
    
    [SerializeReference] public BlackboardVariable<Vector3> FoundBasePosition;
    [SerializeReference] public BlackboardVariable<Vector3> FoundClimbPosition;

    protected override Status OnStart()
    {
        // 1. Setup Layer
        TreeLayer = LayerMask.GetMask("Tree");
        
        if (Target.Value == null) return Status.Failure;

        // 2. Find Trees
        Collider[] hits = Physics.OverlapSphere(Target.Value.Value.transform.position, Radius.Value, TreeLayer);
        
        if (hits.Length == 0) {
            return Status.Failure;
        }

        // 3. Pick Random Tree
        Collider randomTree = hits[UnityEngine.Random.Range(0, hits.Length)];
        Vector3 treeCenter = randomTree.bounds.center;

        // --- STEP 4: FIND BASE POSITION
        Vector3 directionToTree = (treeCenter - Target.Value.Value.transform.position).normalized;
        directionToTree.y = 0; // Flatten direction

        Vector3 rayStartPos = Target.Value.Value.transform.position;
        rayStartPos.y = treeCenter.y; // Raise ray to center height to ensure we hit the trunk

        Vector3 surfaceNormal = -directionToTree; // Default normal
        Vector3 baseSurfacePoint = treeCenter - (directionToTree * randomTree.bounds.extents.x); // Fallback

        RaycastHit treeHit;
        if (randomTree.Raycast(new Ray(rayStartPos, directionToTree), out treeHit, Radius.Value * 2))
        {
            baseSurfacePoint = treeHit.point;
            surfaceNormal = treeHit.normal;
        }
        else 
        {
            baseSurfacePoint = randomTree.ClosestPoint(rayStartPos);
            surfaceNormal = (baseSurfacePoint - treeCenter).normalized;
        }

        Vector3 groundTarget = new Vector3(baseSurfacePoint.x, randomTree.bounds.min.y, baseSurfacePoint.z);
        groundTarget += surfaceNormal * 1.0f; 

        // Snap to NavMesh
        if (NavMesh.SamplePosition(groundTarget, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
        {
            FoundBasePosition.Value = navHit.position;
        }
        else
        {
            FoundBasePosition.Value = groundTarget;
        }

        // --- STEP 5: FIND CLIMB POSITION
        float climbHeight = UnityEngine.Random.Range(minMaxHeight.Value.x, minMaxHeight.Value.y);

        
        float rayHeight = FoundBasePosition.Value.y + climbHeight;
        Vector3 climbRayOrigin = FoundBasePosition.Value;
        climbRayOrigin.y = rayHeight;

        Vector3 rayDirectionIn = (new Vector3(treeCenter.x, rayHeight, treeCenter.z) - climbRayOrigin).normalized;
        climbRayOrigin -= rayDirectionIn * 2.0f; 

        if (randomTree.Raycast(new Ray(climbRayOrigin, rayDirectionIn), out treeHit, 10.0f))
        {
            FoundClimbPosition.Value = treeHit.point + (treeHit.normal * treeClimbOffSet.Value);
        }
        else
        {
            FoundClimbPosition.Value = new Vector3(FoundBasePosition.Value.x, rayHeight, FoundBasePosition.Value.z);
        }

        return Status.Success;
    }
}