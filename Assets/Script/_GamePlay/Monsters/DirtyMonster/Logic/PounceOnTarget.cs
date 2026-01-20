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
    [SerializeReference] public BlackboardVariable<TransformAnchorSO> target; 
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<BoolVariableSO> isMonsterAttached;
    [SerializeReference] public BlackboardVariable<float> PostAttackWait = new BlackboardVariable<float>(2.0f);
    
    [SerializeReference] public BlackboardVariable<float> ShakeIntensity = new BlackboardVariable<float>(0.5f);
    [SerializeReference] public BlackboardVariable<float> RotationSpeed = new BlackboardVariable<float>(20f); 
    
    [SerializeReference] public BlackboardVariable<float> FaceBlockAmount = new BlackboardVariable<float>(0.3f); 

    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> IsFlying;
    [SerializeReference] public BlackboardVariable<string> IsGrabing;
    private bool _hasImpacted;
    private float _waitTimer;
    private Transform _mainCam;

    protected override Status OnStart()
    {
        if (!ShouldAttack.Value) return Status.Success;
        if (Agent.Value == null || target.Value == null) return Status.Failure;

        _hasImpacted = false;
        _waitTimer = 0f;
        isMonsterAttached.Value.Value = true;
        if (Camera.main != null) _mainCam = Camera.main.transform;

        var agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Animator.Value.SetBool(IsFlying.Value, true);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (target.Value == null) return Status.Failure;

        Transform trans = Agent.Value.transform;
        Transform targetTrans = target.Value.Value.transform;

        if (_hasImpacted)
        {
            _waitTimer += Time.deltaTime;

            Vector3 playerHead = targetTrans.position + (Vector3.up * 1.5f);
            Vector3 anchorPos = playerHead;

            if (_mainCam != null)
            {
                anchorPos = Vector3.Lerp(playerHead, _mainCam.position, FaceBlockAmount.Value);
            }

            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * ShakeIntensity.Value;
            trans.position = anchorPos + randomOffset;
            
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
        isMonsterAttached.Value.Value = false;
        Animator.Value.SetBool(IsFlying.Value, false);
        Animator.Value.SetBool(IsGrabing.Value, false);
        
    }
}