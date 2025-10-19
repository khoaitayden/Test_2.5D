using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
namespace CrashKonijn.Goap.MonsterGen
{
// This sensor ONLY provides the PlayerTarget (player's position)
public class PlayerSensor : LocalTargetSensorBase
{
private MonsterConfig config;
public override void Created() { }
    public override void Update() { }

    public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
    {
        // Cache the config on the first run
        if (config == null)
            config = references.GetCachedComponent<MonsterConfig>();

        if (config == null) return null;

        var colliders = new Collider[1];
        var count = Physics.OverlapSphereNonAlloc(
            agent.Transform.position, 
            config.ViewRadius, 
            colliders, 
            config.PlayerLayerMask
        );

        // If no player found, return null (no target)
        if (count == 0)
            return null;
        
        // Return the player's position as the target
        return new PositionTarget(colliders[0].transform.position);
    }
}
}
