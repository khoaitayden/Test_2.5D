using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Watch And Detect Player", story: "Watch for [Target] (Set [IsSpotted])", category: "AI", id: "watchdetect")]
public partial class WatchAndDetectPlayer : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    [SerializeReference] public BlackboardVariable<float> Range;
    [SerializeReference] public BlackboardVariable<float> Angle;
    [SerializeReference] public BlackboardVariable<float> Duration;
    [SerializeReference] public BlackboardVariable<float> LightTiltAngle; 

    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> IsGrabing;
    [SerializeReference] public BlackboardVariable<Light> eyeLight;
    [SerializeReference] public BlackboardVariable<float> lightIntensity;

    [SerializeReference] public BlackboardVariable<bool> IsSpotted;

    private float _timer;
    private float _warmupTimer; // New Timer
    private const float WARMUP_DURATION = 1.5f;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null) return Status.Failure;

        if (Animator.Value != null)
            Animator.Value.SetBool(IsGrabing.Value, true);

        if (eyeLight.Value != null)
        {
            eyeLight.Value.enabled = true;
            eyeLight.Value.intensity = lightIntensity.Value;
            eyeLight.Value.range = Range.Value;
            eyeLight.Value.spotAngle = Angle.Value;
            UpdateLightRotation();
        }

        _timer = 0f;
        _warmupTimer = 0f; // Reset buffer
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float dt = Time.deltaTime;
        _timer += dt;
        _warmupTimer += dt;

        Vector3 currentLookDir = UpdateLightRotation();

        // 1. Check Timeout
        if (_timer >= Duration.Value)
        {
            IsSpotted.Value = false; 
            return Status.Success; 
        }

        // 2. Wait for Warmup
        if (_warmupTimer < WARMUP_DURATION)
        {
            // Light is ON, but we ignore the player
            return Status.Running; 
        }

        // 3. Check Visibility (Only runs after 1 second)
        if (CheckVisibility(currentLookDir))
        {
            IsSpotted.Value = true; 
            return Status.Success; 
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (eyeLight.Value != null)
        {
            eyeLight.Value.intensity = 0;
            eyeLight.Value.enabled = false;
        }

        if (Animator.Value != null)
        {
            Animator.Value.SetBool(IsGrabing.Value, false);
        }
    }

    private Vector3 GetTiltedDirection()
    {
        Vector3 direction = Vector3.down;
        if (Agent.Value != null)
        {
            direction = Quaternion.AngleAxis(LightTiltAngle.Value, Agent.Value.transform.right) * direction;
        }
        return direction;
    }

    private Vector3 UpdateLightRotation()
    {
        Vector3 lookDir = GetTiltedDirection();
        if (eyeLight.Value != null)
        {
            eyeLight.Value.transform.rotation = Quaternion.LookRotation(lookDir, Agent.Value.transform.up);
        }
        return lookDir;
    }

    private bool CheckVisibility(Vector3 currentLookDir)
    {
        Vector3 eyePos = (eyeLight.Value != null) ? eyeLight.Value.transform.position : Agent.Value.transform.position;
        Vector3 targetPos = Target.Value.transform.position;
        
        Vector3 dirToTarget = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        if (distToTarget > Range.Value) return false;
        if (Vector3.Angle(currentLookDir, dirToTarget) > Angle.Value / 2f) return false;

        return true;
    }
}