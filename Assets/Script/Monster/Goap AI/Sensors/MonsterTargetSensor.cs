using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterTargetSensor : LocalTargetSensorBase
{
    public float patrolRadius = 20f;

    public override void Created() { }
    
    public override void Update() { }

    public override ITarget Sense(IActionReceiver receiver, IComponentReference references, ITarget existingTarget)
    {
        var agent = receiver as IMonoAgent;
        
        // Generate random patrol point around agent
        Vector2 random = Random.insideUnitCircle * patrolRadius;
        Vector3 patrolPoint = agent.Transform.position + new Vector3(random.x, 0, random.y);
        
        return new PositionTarget(patrolPoint);
    }
}