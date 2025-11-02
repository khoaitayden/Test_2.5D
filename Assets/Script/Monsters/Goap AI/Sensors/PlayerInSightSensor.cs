// FILE TO EDIT: PlayerInSightSensor.cs (Corrected Version)
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class PlayerInSightSensor : LocalWorldSensorBase
    {
        private MonsterConfig config;
        
        // This is the default Sense method for the GOAP runner.
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            // Now this call is valid because the types match perfectly.
            return IsPlayerInSight(agent, references.GetCachedComponent<MonsterConfig>());
        }

        // #### THE ONLY CHANGE IS ON THIS LINE ####
        // We now accept the more general 'IActionReceiver', which works for both calls.
        public static bool IsPlayerInSight(IActionReceiver agent, MonsterConfig config)
        {
            if (config == null) return false;

            Vector3 eyesPosition = agent.Transform.position + Vector3.up * 0.5f;

            var colliders = new Collider[10];
            int count = Physics.OverlapSphereNonAlloc(
                eyesPosition,
                config.viewRadius,
                colliders,
                config.playerLayerMask
            );

            if (count == 0) return false;

            for (int i = 0; i < count; i++)
            {
                Transform target = colliders[i].transform;
                Vector3 toTarget = target.position - eyesPosition;
                float distanceToTarget = toTarget.magnitude;

                if (distanceToTarget > config.viewRadius) continue;

                // Check if within view cone
                if (Vector3.Angle(agent.Transform.forward, toTarget) > config.ViewAngle / 2f)
                    continue;

                Vector3 targetDir = toTarget.normalized;
                int rayCount = Mathf.Max(1, config.numOfRayCast);
                float halfAngle = config.ViewAngle / 2f;

                float raySpreadAngle = Mathf.Min(30f, config.ViewAngle); // Max 10° spread for accuracy
                float rayHalfSpread = raySpreadAngle / 2f;

                bool hasClearRay = false;

                for (int r = 0; r < rayCount; r++)
                {
                    // Distribute from -rayHalfSpread to +rayHalfSpread
                    float t = rayCount > 1 ? r / (float)(rayCount - 1) : 0.5f;
                    float angleOffset = Mathf.Lerp(-rayHalfSpread, rayHalfSpread, t);

                    // Create a rotation around the UP axis (Y) by angleOffset
                    Quaternion spreadRotation = Quaternion.Euler(0, angleOffset, 0);
                    Vector3 rayDir = spreadRotation * targetDir;

                    // Optional: also add vertical spread if needed (usually not for 2D-like AI)

                    // Draw debug ray
                    Debug.DrawRay(eyesPosition, rayDir * distanceToTarget, Color.cyan);

                    // Cast ray
                    if (!Physics.Raycast(eyesPosition, rayDir, distanceToTarget, config.obstacleLayerMask))
                    {
                        // This ray reached the target without hitting an obstacle!
                        hasClearRay = true;
                        break;
                    }
                }

                if (hasClearRay)
                {
                    Debug.DrawLine(eyesPosition, target.position, Color.green);
                    return true; // Can see this target
                }
            }

            return false;
        }

        public override void Created() { }
        public override void Update() { }
    }
}