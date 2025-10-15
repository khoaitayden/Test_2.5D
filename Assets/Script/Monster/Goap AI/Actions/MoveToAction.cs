using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;

public class MoveToAction : GoapActionBase<CommonData>
{
    public override void Start(IMonoAgent agent, CommonData data) { }
    
    public override IActionRunState Perform(IMonoAgent agent, CommonData data, IActionContext context)
    {
        return ActionRunState.Continue;
    }
    
    public override void End(IMonoAgent agent, CommonData data) { }
}