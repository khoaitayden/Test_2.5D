using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Watch And Detect Player", story: "Watch for [Target] (Set [IsSpotted])", category: "AI", id: "watchdetect")]
public partial class WatchAndDetectPlayer : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Range = new BlackboardVariable<float>();
    [SerializeReference] public BlackboardVariable<float> Angle = new BlackboardVariable<float>();
    [SerializeReference] public BlackboardVariable<float> Duration = new BlackboardVariable<float>();

    // Output Variable
    [SerializeReference] public BlackboardVariable<bool> IsSpotted;

    private float _timer;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null) return Status.Failure;
        _timer = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _timer += Time.deltaTime;

        // 1. Check Visibility
        if (CheckVisibility())
        {
            IsSpotted.Value = true; // Saw them!
            return Status.Success;  // Done.
        }

        // 2. Check Timeout
        if (_timer >= Duration.Value)
        {
            IsSpotted.Value = false; // Didn't see them.
            return Status.Success;   // Done (Success means "Finished Watching", not "Found Player")
        }

        return Status.Running;
    }

    private bool CheckVisibility()
    {
        Vector3 eyePos = Agent.Value.transform.position;
        Vector3 targetPos = Target.Value.transform.position;
        Vector3 dirToTarget = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        if (distToTarget > Range.Value) return false;
        // Looking Down check
        if (Vector3.Angle(Vector3.down, dirToTarget) > Angle.Value / 2f) return false;

        return true;
    }
    
    // (Keep OnDrawGizmos same as before)
    public void OnDrawGizmos()
    {
        if (Agent.Value == null) return;
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Vector3 origin = Agent.Value.transform.position;
        Vector3 down = Vector3.down;
        Gizmos.DrawRay(origin, down * Range.Value);
    }
}