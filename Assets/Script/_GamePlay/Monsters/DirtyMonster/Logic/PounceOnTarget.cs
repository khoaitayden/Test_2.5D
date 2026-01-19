using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Pounce On Target", story: "If [ShouldAttack] is true, pounce on [Target] and go chaos", category: "Combat", id: "pounce")]
public partial class PounceOnTarget : Action
{
    [SerializeReference] public BlackboardVariable<bool> ShouldAttack;
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<float> PostAttackWait = new BlackboardVariable<float>(2.0f);
    
    // CHAOS SETTINGS
    [SerializeReference] public BlackboardVariable<float> ShakeIntensity = new BlackboardVariable<float>(0.5f);
    [SerializeReference] public BlackboardVariable<float> RotationSpeed = new BlackboardVariable<float>(20f); 
    
    // NEW: How much to block the camera?
    // 0.2 = Close to player face. 0.8 = Close to Camera.
    [SerializeReference] public BlackboardVariable<float> FaceBlockAmount = new BlackboardVariable<float>(0.3f); 

    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> AnimBool = new BlackboardVariable<string>("IsAttacking");
    private bool _hasImpacted;
    private float _waitTimer;
    private float _chaosTimer;
    private Transform _mainCam; // Cache camera

    protected override Status OnStart()
    {
        if (!ShouldAttack.Value) return Status.Success;
        if (Agent.Value == null || Target.Value == null) return Status.Failure;

        _hasImpacted = false;
        _waitTimer = 0f;
        _chaosTimer = 0f;
        
        if (Camera.main != null) _mainCam = Camera.main.transform;

        var agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        if (Animator.Value != null) Animator.Value.SetBool(AnimBool.Value, true);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null) return Status.Failure;

        Transform trans = Agent.Value.transform;
        Transform targetTrans = Target.Value.transform;

        if (_hasImpacted)
        {
            _waitTimer += Time.deltaTime;

            // 1. Calculate Center Point (Between Player Head and Camera)
            Vector3 playerHead = targetTrans.position + (Vector3.up * 1.5f); // Approx head height
            Vector3 anchorPos = playerHead; // Default if no camera

            if (_mainCam != null)
            {
                // Linear Interpolation: 0 = Player, 1 = Camera
                // 0.3f is usually a good "In your face" spot without clipping
                anchorPos = Vector3.Lerp(playerHead, _mainCam.position, FaceBlockAmount.Value);
            }

            // 2. Add Position Chaos (Jitter)
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * ShakeIntensity.Value;
            trans.position = anchorPos + randomOffset;

            // 3. Add Rotation Chaos
            // Look AT the camera (scary!) or AT the player (biting)?
            // Let's look at the Player so we see the monster's back/top? 
            // Actually, for a face hugger, we usually want the monster's belly facing the camera.
            // Let's Look At the Camera to scream at the player.
            
            Quaternion lookDir;
            if (_mainCam != null)
            {
                lookDir = Quaternion.LookRotation(_mainCam.position - trans.position);
            }
            else
            {
                lookDir = Quaternion.LookRotation(targetTrans.position - trans.position);
            }
            
            Quaternion randomTwist = Quaternion.Euler(
                UnityEngine.Random.Range(-45, 45), 
                UnityEngine.Random.Range(-180, 180), 
                UnityEngine.Random.Range(-45, 45)
            );

            trans.rotation = Quaternion.Slerp(trans.rotation, lookDir * randomTwist, Time.deltaTime * RotationSpeed.Value);

            if (_waitTimer >= PostAttackWait.Value)
            {
                return Status.Success;
            }
            return Status.Running;
        }

        // --- PHASE 1: HOMING POUNCE ---
        Vector3 targetPos = targetTrans.position + (Vector3.up * 1.5f); // Aim for head
        trans.position = Vector3.MoveTowards(trans.position, targetPos, Speed.Value * Time.deltaTime);

        Vector3 dir = (targetPos - trans.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            trans.rotation = Quaternion.Slerp(trans.rotation, look, Time.deltaTime * 15f);
        }

        if (Vector3.Distance(trans.position, targetPos) < 1.2f)
        {
            _hasImpacted = true;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Animator.Value != null) Animator.Value.SetBool(AnimBool.Value, false);
    }
}