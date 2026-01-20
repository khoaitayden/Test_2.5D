using UnityEngine;
using Unity.Cinemachine;

public class PlayerVisionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [SerializeField] private BoolVariableSO isMonsterAttached;

    [Header("References")]
    [SerializeField] private FlashlightController flashlight;
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Camera Settings")]
    [SerializeField] private float baseFarClip = 40f;       
    [SerializeField] private float flashlightFarClip = 65f; 
    [SerializeField] private float deadFarClip = 15f;

    [SerializeField] private float blindedFarClip = 2.0f;       

    void Update()
    {
        // 1. Calculate Standard Logic
        float energyFactor = 0f;
        if(maxEnergy.Value > 0) energyFactor = currentEnergy.Value / maxEnergy.Value;
        
        bool hasEnergy = currentEnergy.Value > 0;

        float targetClip = deadFarClip;

        if (isMonsterAttached != null && isMonsterAttached.Value)
        {
            targetClip = blindedFarClip;
        }
        else if (hasEnergy && flashlight != null && flashlight.IsActive)
        {
            targetClip = flashlightFarClip;
        }

        else if (hasEnergy)
        {
            targetClip = Mathf.Lerp(deadFarClip, baseFarClip, energyFactor);
        }

        float lerpSpeed = (targetClip == blindedFarClip) ? 10f : 2f;

        var lens = virtualCamera.Lens;
        lens.FarClipPlane = Mathf.Lerp(lens.FarClipPlane, targetClip, Time.deltaTime * lerpSpeed);
        virtualCamera.Lens = lens;
    }
}