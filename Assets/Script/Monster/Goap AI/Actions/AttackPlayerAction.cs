using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class AttackPlayerAction : GoapActionBase<CommonData>
{
    public override void Start(IMonoAgent agent, CommonData data)
    {
        data.Timer = 1.5f; 
        Debug.Log("Starting Attack!");
    }

    public override IActionRunState Perform(IMonoAgent agent, CommonData data, IActionContext context)
    {
        data.Timer -= context.DeltaTime;

        if (data.Timer > 0)
        {
            agent.Transform.LookAt(data.Target.Position);
            return ActionRunState.Continue;
        }

        Debug.Log("Player Killed!");
        return ActionRunState.Completed;
    }

    public override void End(IMonoAgent agent, CommonData data) { }
}