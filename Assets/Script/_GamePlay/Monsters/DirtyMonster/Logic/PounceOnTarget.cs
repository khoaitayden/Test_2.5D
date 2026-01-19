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
    [SerializeReference] public BlackboardVariable<float> ShakeIntensity = new BlackboardVariable<float>(0.5f); // How far it shakes
    [SerializeReference] public BlackboardVariable<float> RotationSpeed = new BlackboardVariable<float>(20f); // How fast it twists
    
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> AnimBool = new BlackboardVariable<string>("IsAttacking");

    private bool _hasImpacted;
    private float _waitTimer;
    private float _chaosTimer;

    protected override Status OnStart()
    {
        if (!ShouldAttack.Value) return Status.Success;
        if (Agent.Value == null || Target.Value == null) return Status.Failure;

        _hasImpacted = false;
        _waitTimer = 0f;
        _chaosTimer = 0f;

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

        // --- PHASE 2: CHAOS MODE (Attached) ---
        if (_hasImpacted)
        {
            _waitTimer += Time.deltaTime;
            _chaosTimer += Time.deltaTime * 10f; // Speed up the noise

            // 1. Stick to Player (Chest height approx 1.0f up)
            Vector3 anchorPos = targetTrans.position + (Vector3.up * 1.0f);

            // 2. Add Position Chaos (Jitter)
            // We use Perlin noise to make it look organic, or Random for violent shaking.
            // Let's use Random inside sphere for violent "Mauling" look.
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * ShakeIntensity.Value;
            trans.position = anchorPos + randomOffset;

            // 3. Add Rotation Chaos (Flailing)
            // Snap to look at player, then add random twists
            Quaternion lookAtPlayer = Quaternion.LookRotation(targetTrans.position - trans.position);
            
            // Generate a random chaotic rotation
            Quaternion randomTwist = Quaternion.Euler(
                UnityEngine.Random.Range(-45, 45), 
                UnityEngine.Random.Range(-180, 180), 
                UnityEngine.Random.Range(-45, 45)
            );

            // Lerp wildly between looking at player and twisting
            trans.rotation = Quaternion.Slerp(trans.rotation, lookAtPlayer * randomTwist, Time.deltaTime * RotationSpeed.Value);

            if (_waitTimer >= PostAttackWait.Value)
            {
                return Status.Success;
            }
            return Status.Running;
        }

        // --- PHASE 1: HOMING POUNCE ---
        Vector3 targetPos = targetTrans.position;
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