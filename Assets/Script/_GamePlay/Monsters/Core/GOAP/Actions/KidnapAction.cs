using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen
{
    public class KidnapAction : GoapActionBase<KidnapAction.Data>
    {
        private KidnapMonsterConfig config;
        private MonsterMovement movement;
        private MonsterBrain brain;
        private Transform playerTransform;

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            movement = agent.GetComponent<MonsterMovement>();
            brain = agent.GetComponent<MonsterBrain>();
            
            data.startTime = Time.time; 

            if (data.Target is TransformTarget tt)
            {
                playerTransform = tt.Transform;
                movement.Chase(playerTransform, config.chaseSpeed);
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (Time.time > data.startTime + config.maxChaseTime)
            {
                brain?.OnMovementStuck();
                if (brain != null) brain.WipeMemory();
                return ActionRunState.Stop;
            }

            if (playerTransform == null) return ActionRunState.Stop;

            if (Vector3.Distance(agent.Transform.position, playerTransform.position) < 2.0f)
            {
                KidnapPlayer();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
        }

        private void KidnapPlayer()
        {
            // 1. Drain Energy
            if (config != null && config.currentEnergy != null && config.maxEnergy != null)
            {
                float drainAmount = config.maxEnergy.Value * config.energyDrainPercent;
                config.currentEnergy.ApplyChange(-drainAmount, 0f, config.maxEnergy.Value);
            }

            // 2. Find Furthest Valid Point on NavMesh
            Vector3 finalPos = FindFurthestNavMeshPoint();

            // 3. Teleport
            var controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            
            playerTransform.position = finalPos;
            
            if (controller != null) controller.enabled = true;

            Debug.Log($"Player Kidnapped to {finalPos}!");
            
            if (brain != null) brain.WipeMemory();
        }

        private Vector3 FindFurthestNavMeshPoint()
        {
            // A. Determine what we are running AWAY from
            Vector3 avoidPoint = Vector3.zero; // Default center

            bool holdingItem = config.isCarryingItem != null && config.isCarryingItem.Value;

            if (holdingItem)
            {
                // If holding item, run away from Beacon (0,0,0)
                if (config.beaconAnchor != null && config.beaconAnchor.Value != null)
                    avoidPoint = config.beaconAnchor.Value.position;
            }
            else
            {
                // If empty handed, run away from the remaining Objectives
                if (config.activeObjectivesSet != null)
                {
                    var items = config.activeObjectivesSet.GetItems();
                    if (items.Count > 0)
                    {
                        Vector3 sum = Vector3.zero;
                        foreach (var item in items) if (item != null) sum += item.position;
                        avoidPoint = sum / items.Count; // Center of objectives
                    }
                }
            }

            // B. Sample random points and pick the furthest one
            Vector3 bestPoint = playerTransform.position; // Fallback
            float maxDistanceSqr = -1f;

            for (int i = 0; i < config.teleportSampleAttempts; i++)
            {
                // Pick random point in map radius
                Vector2 rndCircle = Random.insideUnitCircle * config.mapRadius;
                Vector3 attemptPos = new Vector3(rndCircle.x, 0, rndCircle.y);

                // Check if it hits NavMesh (using a wide check to snap to nearest floor)
                if (NavMesh.SamplePosition(attemptPos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                {
                    float dSqr = (hit.position - avoidPoint).sqrMagnitude;
                    
                    // We want the point with the HIGHEST distance from avoidPoint
                    if (dSqr > maxDistanceSqr)
                    {
                        maxDistanceSqr = dSqr;
                        bestPoint = hit.position;
                    }
                }
            }

            // Lift slightly to prevent clipping floor
            return bestPoint + Vector3.up * 1.0f;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}