using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;
namespace CrashKonijn.Goap.MonsterGen
{
// Back to LocalWorldSensorBase - simpler and works
public class PlayerInSightSensor : LocalWorldSensorBase
{
private MonsterConfig config;
public override void Created() 
    {
        Debug.Log("[PlayerInSightSensor] Sensor CREATED!");
    }
    
    public override void Update() 
    {
        // Empty
    }

    public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
    {
        // Cache the config on the first run
        if (config == null)
            config = references.GetCachedComponent<MonsterConfig>();

        if (config == null)
        {
            Debug.LogError("[PlayerInSightSensor] MonsterConfig is NULL!");
            return false;
        }

        // Debug info every 2 seconds
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[PlayerInSightSensor] Checking... ViewRadius: {config.ViewRadius}, LayerMask: {config.PlayerLayerMask.value}");
        }

        var colliders = new Collider[10];
        var count = Physics.OverlapSphereNonAlloc(
            agent.Transform.position, 
            config.ViewRadius, 
            colliders, 
            config.PlayerLayerMask
        );

        // Debug logging
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                if (colliders[i] != null)
                {
                    Debug.Log($"[PlayerInSightSensor] PLAYER DETECTED! Name: {colliders[i].name}, Distance: {Vector3.Distance(agent.Transform.position, colliders[i].transform.position):F2}");
                }
            }
        }

        // Return true if player is in sight, false otherwise
        return count > 0;
    }
}
}
