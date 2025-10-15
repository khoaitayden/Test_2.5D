using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class PlayerTargetSensor : LocalTargetSensorBase
{
    public float sensorRadius = 10f;
    public LayerMask playerLayerMask;

    public override void Created() { }
    public override void Update() { }

    // signature for target sensors in v3:
    public override ITarget Sense(IActionReceiver receiver, IComponentReference references, ITarget existingTarget)
    {
        var agentTransform = (receiver as IMonoAgent)?.Transform;
        if (agentTransform == null) return null;

        Collider[] colliders = new Collider[1];
        int hits = Physics.OverlapSphereNonAlloc(agentTransform.position, sensorRadius, colliders, playerLayerMask);
        if (hits > 0)
        {
            var playerT = colliders[0].transform;
            // return a TransformTarget (keeps following if target moves)
            return new TransformTarget(playerT);
        }

        return null;
    }
}
