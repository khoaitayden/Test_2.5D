// FILE TO EDIT: PlayerCurrentPosSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerCurrentPosSensor : LocalTargetSensorBase
    {
        private Transform playerTransform;

        // The original Sense method now just uses our new helper method.
        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            return GetPlayerTarget();
        }

        // #### NEW PUBLIC HELPER METHOD ####
        // This is a simple, clean way for the MonsterBrain to ask "where is the player?"
        public ITarget GetPlayerTarget()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
                else
                {
                    // No player found, so no target to return.
                    return null;
                }
            }
            return new TransformTarget(playerTransform);
        }

        public override void Created() { }
        public override void Update() { }
    }
}