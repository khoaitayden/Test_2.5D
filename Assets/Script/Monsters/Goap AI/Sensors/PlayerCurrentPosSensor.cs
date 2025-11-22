using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerCurrentPosSensor : LocalTargetSensorBase
    {
        // Cache the transform so we don't search for it 60 times a second
        private static Transform _cachedPlayer;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            // 1. Find Player if we lost the reference
            if (_cachedPlayer == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    _cachedPlayer = playerObj.transform;
                }
            }

            // 2. If Player is completely missing (Game Over?), fallback to self
            if (_cachedPlayer == null)
            {
                return new PositionTarget(agent.Transform.position);
            }

            // 3. !!! THIS IS THE FIX !!!
            // You MUST return 'TransformTarget'.
            // If you return 'PositionTarget', the monster will run to the first spot it saw and stop.
            return new TransformTarget(_cachedPlayer);
        }
    }
}