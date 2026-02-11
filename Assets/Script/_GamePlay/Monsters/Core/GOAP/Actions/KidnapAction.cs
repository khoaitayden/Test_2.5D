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
        private KidnapClawController clawController; 
        private Transform playerTransform;

        // Constants
        private const float CONTACT_DISTANCE = 4f; 

        public override void Created() { }

        public override void Start(IMonoAgent agent, Data data)
        {
            config = agent.GetComponent<KidnapMonsterConfig>();
            movement = agent.GetComponent<MonsterMovement>();
            brain = agent.GetComponent<MonsterBrain>();
            clawController = agent.GetComponent<KidnapClawController>();
            
            data.startTime = Time.time; 

            if (data.Target is TransformTarget tt)
            {
                playerTransform = tt.Transform;
                movement.Chase(playerTransform, config.chaseSpeed);
            }
            
            Debug.Log("[KidnapAction] Started chasing player.");
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

            float dist = Vector3.Distance(agent.Transform.position, playerTransform.position);
            if (clawController==null)Debug.Log("LMAO");
            if (clawController != null)
            {
                float t = Mathf.InverseLerp(config.grabPreparationDistance, CONTACT_DISTANCE, dist);
                Debug.Log($"Claw Blend: {t}");
                clawController.UpdateClawBlend(t, playerTransform);
            }

            if (dist < CONTACT_DISTANCE)
            {
                KidnapPlayer();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent agent, Data data)
        {
            movement.Stop();
            if (clawController != null)
            {
                clawController.UpdateClawBlend(0f, null);
            }
        }

        private void KidnapPlayer()
        {
            // 1. Drain Energy
            if (config != null && config.currentEnergy != null && config.maxEnergy != null)
            {
                float drainAmount = config.maxEnergy.Value * config.energyDrainPercent;
                config.currentEnergy.ApplyChange(-drainAmount, 0f, config.maxEnergy.Value);
            }

            // 2. Teleport Logic
            Vector3 finalPos = FindFurthestNavMeshPoint();

            var controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            playerTransform.position = finalPos;
            if (controller != null) controller.enabled = true;

            Debug.Log($"[KidnapAction] Player Kidnapped to {finalPos}!");
            
            // 3. Reset Brain
            if (brain != null) brain.WipeMemory();
        }

        private Vector3 FindFurthestNavMeshPoint()
        {
            Vector3 avoidPoint = Vector3.zero;

            bool holdingItem = config.isCarryingItem != null && config.isCarryingItem.Value;

            if (holdingItem)
            {
                if (config.beaconAnchor != null && config.beaconAnchor.Value != null)
                    avoidPoint = config.beaconAnchor.Value.position;
            }
            else
            {
                if (config.activeObjectivesSet != null)
                {
                    var items = config.activeObjectivesSet.GetItems();
                    if (items.Count > 0)
                    {
                        Vector3 sum = Vector3.zero;
                        foreach (var item in items) if (item != null) sum += item.position;
                        avoidPoint = sum / items.Count;
                    }
                }
            }

            Vector3 bestPoint = playerTransform.position;
            float maxDistanceSqr = -1f;

            for (int i = 0; i < config.teleportSampleAttempts; i++)
            {
                Vector2 rndCircle = Random.insideUnitCircle * config.mapRadius;
                Vector3 attemptPos = new Vector3(rndCircle.x, 0, rndCircle.y);

                if (NavMesh.SamplePosition(attemptPos, out NavMeshHit hit, 25f, NavMesh.AllAreas))
                {
                    float dSqr = (hit.position - avoidPoint).sqrMagnitude;
                    
                    if (dSqr > maxDistanceSqr)
                    {
                        maxDistanceSqr = dSqr;
                        bestPoint = hit.position;
                    }
                }
            }

            return bestPoint + Vector3.up * 1.0f;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float startTime;
        }
    }
}