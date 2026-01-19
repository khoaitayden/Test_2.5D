using UnityEngine;
using Unity.Cinemachine;

public class PlayerVisionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    
    // NEW: The override switch
    [SerializeField] private BoolVariableSO isMonsterAttached; // Drag "var_IsMonsterAttached"

    [Header("References")]
    [SerializeField] private FlashlightController flashlight;
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Camera Settings")]
    [SerializeField] private float baseFarClip = 40f;       
    [SerializeField] private float flashlightFarClip = 65f; 
    [SerializeField] private float deadFarClip = 15f;
    
    // NEW: The "Jump Scare" distance
    [SerializeField] private float blindedFarClip = 2.0f;       

    void Update()
    {
        // 1. Calculate Standard Logic
        float energyFactor = 0f;
        if(maxEnergy.Value > 0) energyFactor = currentEnergy.Value / maxEnergy.Value;
        
        bool hasEnergy = currentEnergy.Value > 0;

        float targetClip = deadFarClip;

        // 2. Determine Target Clip Plane
        
        // PRIORITY 1: Monster Attack (Blindness)
        if (isMonsterAttached != null && isMonsterAttached.Value)
        {
            targetClip = blindedFarClip;
        }
        // PRIORITY 2: Flashlight
        else if (hasEnergy && flashlight != null && flashlight.IsActive)
        {
            targetClip = flashlightFarClip;
        }
        // PRIORITY 3: Normal / Low Energy
        else if (hasEnergy)
        {
            targetClip = Mathf.Lerp(deadFarClip, baseFarClip, energyFactor);
        }
        // else: defaults to deadFarClip

        // 3. Apply Smoothly
        // Use a faster lerp speed if blinded to make the scare sudden
        float lerpSpeed = (targetClip == blindedFarClip) ? 10f : 2f;

        var lens = virtualCamera.Lens;
        lens.FarClipPlane = Mathf.Lerp(lens.FarClipPlane, targetClip, Time.deltaTime * lerpSpeed);
        virtualCamera.Lens = lens;
    }
}