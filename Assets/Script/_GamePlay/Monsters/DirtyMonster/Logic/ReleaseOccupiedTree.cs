using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Release Occupied Tree", story: "Remove [Tree] from [OccupiedSet]", category: "Variable", id: "releasetree")]
public partial class ReleaseOccupiedTree : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Tree;
    [SerializeReference] public BlackboardVariable<TransformSetSO> OccupiedSet;

    protected override Status OnStart()
    {
        if (OccupiedSet.Value != null && Tree.Value != null)
        {
            OccupiedSet.Value.Remove(Tree.Value);
        }
        return Status.Success;
    }
}