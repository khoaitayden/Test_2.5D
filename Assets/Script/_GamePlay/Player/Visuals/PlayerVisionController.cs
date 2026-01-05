using UnityEngine;
using Unity.Cinemachine;

public class PlayerVisionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [Header("References")]
    [Tooltip("The Flashlight Script on the Player or Camera.")]
    [SerializeField] private FlashlightController flashlight;
    
    [Tooltip("The Cinemachine Camera to adjust.")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Camera Settings")]
    [SerializeField] private float baseFarClip = 40f;       
    [SerializeField] private float flashlightFarClip = 65f; 
    [SerializeField] private float deadFarClip = 15f;       

    void Update()
    {

        float energyFactor =currentEnergy.Value / maxEnergy.Value;
        bool hasEnergy = currentEnergy.Value > 0;

        // Calculate Target Clip Plane
        float targetClip = deadFarClip;

        if (hasEnergy)
        {
            if (flashlight != null && flashlight.IsActive)
            {
                targetClip = flashlightFarClip;
            }
            else
            {
                targetClip = Mathf.Lerp(deadFarClip, baseFarClip, energyFactor);
            }
        }

        // Apply to Cinemachine Smoothly
        var lens = virtualCamera.Lens;
        lens.FarClipPlane = Mathf.Lerp(lens.FarClipPlane, targetClip, Time.deltaTime * 2f);
        virtualCamera.Lens = lens;
    }
}