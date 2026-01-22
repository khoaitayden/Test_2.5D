using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find Tree Near Target", story: "Find tree near [Target] (Avoids [OccupiedSet])", category: "Variable", id: "findtree")]
public partial class FindTreeNearTarget : Action
{
    [SerializeReference] public BlackboardVariable<TransformAnchorSO> Target;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<float> treeClimbOffSet; 
    [SerializeReference] public BlackboardVariable<Vector2> minMaxHeight;
    
    // NEW: The Registry
    [SerializeReference] public BlackboardVariable<TransformSetSO> OccupiedSet; 
    
    [SerializeReference] public BlackboardVariable<Vector3> FoundBasePosition;
    [SerializeReference] public BlackboardVariable<Vector3> FoundClimbPosition;
    
    // NEW: Output the specific tree we found so we can unregister it later
    [SerializeReference] public BlackboardVariable<Transform> FoundTreeTransform; 

    private LayerMask TreeLayer;

    protected override Status OnStart()
    {
        TreeLayer = LayerMask.GetMask("Tree");
        
        if (Target.Value == null || Target.Value.Value == null) 
        {
            return Status.Failure;
        }

        Collider[] hits = Physics.OverlapSphere(Target.Value.Value.transform.position, Radius.Value, TreeLayer);
        
        if (hits.Length == 0) return Status.Failure;

        // Shuffle array for randomness (Fisher-Yates)
        for (int i = 0; i < hits.Length; i++) {
            Collider temp = hits[i];
            int r = UnityEngine.Random.Range(i, hits.Length);
            hits[i] = hits[r];
            hits[r] = temp;
        }

        Collider selectedTree = null;

        // Find first free tree
        foreach(var hit in hits)
        {
            if (OccupiedSet.Value != null)
            {
                // Is this tree already taken?
                if (OccupiedSet.Value.GetItems().Contains(hit.transform))
                {
                    continue; // Skip
                }
            }
            
            selectedTree = hit;
            break;
        }

        if (selectedTree == null)
        {
            Debug.LogWarning("All nearby trees are occupied!");
            return Status.Failure;
        }

        // REGISTER TREE
        if (OccupiedSet.Value != null)
        {
            OccupiedSet.Value.Add(selectedTree.transform);
        }
        
        FoundTreeTransform.Value = selectedTree.transform; // Save ref for later cleanup

        // --- Calculate Positions (Same logic as before) ---
        Vector3 treeCenter = selectedTree.bounds.center;
        Vector3 directionToTree = (treeCenter - Target.Value.Value.transform.position).normalized;
        directionToTree.y = 0; 

        Vector3 rayStartPos = Target.Value.Value.transform.position;
        rayStartPos.y = treeCenter.y; 

        Vector3 surfaceNormal = -directionToTree; 
        Vector3 baseSurfacePoint = treeCenter;

        RaycastHit treeHit;
        if (selectedTree.Raycast(new Ray(rayStartPos, directionToTree), out treeHit, Radius.Value * 2))
        {
            baseSurfacePoint = treeHit.point;
            surfaceNormal = treeHit.normal;
        }
        else 
        {
            baseSurfacePoint = selectedTree.ClosestPoint(rayStartPos);
            surfaceNormal = (baseSurfacePoint - treeCenter).normalized;
        }

        Vector3 groundTarget = new Vector3(baseSurfacePoint.x, selectedTree.bounds.min.y, baseSurfacePoint.z);
        groundTarget += surfaceNormal * 1.5f; 

        if (NavMesh.SamplePosition(groundTarget, out NavMeshHit navHit, 5.0f, NavMesh.AllAreas))
        {
            FoundBasePosition.Value = navHit.position;
        }
        else
        {
            FoundBasePosition.Value = groundTarget;
        }

        float climbHeight = UnityEngine.Random.Range(minMaxHeight.Value.x, minMaxHeight.Value.y);
        float rayHeight = FoundBasePosition.Value.y + climbHeight;
        Vector3 climbRayOrigin = FoundBasePosition.Value;
        climbRayOrigin.y = rayHeight;
        Vector3 rayDirectionIn = (new Vector3(treeCenter.x, rayHeight, treeCenter.z) - climbRayOrigin).normalized;
        climbRayOrigin -= rayDirectionIn * 2.0f; 

        if (selectedTree.Raycast(new Ray(climbRayOrigin, rayDirectionIn), out treeHit, 10.0f))
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