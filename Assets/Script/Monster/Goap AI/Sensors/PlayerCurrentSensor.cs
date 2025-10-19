// FILE TO EDIT: PlayerCurrentPosSensor.cs (or whatever you named it)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    // This sensor is now omniscient. It ALWAYS knows where the player is.
    public class PlayerCurrentPosSensor : LocalTargetSensorBase
    {
        // We will cache the player's transform for high performance.
        private Transform playerTransform;

        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            // If we haven't found the player yet, find them ONCE and cache it.
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                
                if (player != null)
                {
                    playerTransform = player.transform;
                }
                else
                {
                    // If there's no player, we can't sense a target.
                    // The system will keep trying until a player with the tag appears.
                    Debug.LogWarning("PlayerCurrentPosSensor: Could not find GameObject with tag 'Player'");
                    return null;
                }
            }
            
            // Return a TransformTarget using our cached reference. This is very fast.
            return new TransformTarget(playerTransform);
        }
    }
}