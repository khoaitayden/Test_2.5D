using UnityEngine;
using Unity.Cinemachine;

public class PlayerVisionController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Flashlight Script on the Player or Camera.")]
    [SerializeField] private FlashlightController flashlight;
    
    [Tooltip("The Cinemachine Camera to adjust.")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Camera Settings")]
    [SerializeField] private float baseFarClip = 40f;       // Normal range (High Energy)
    [SerializeField] private float flashlightFarClip = 65f; // Boosted range (Flashlight ON)
    [SerializeField] private float deadFarClip = 15f;       // Blindness range (No Energy)

    void Update()
    {
        if (LightEnergyManager.Instance == null || virtualCamera == null) return;

        // Get Energy Data
        float energyFactor = LightEnergyManager.Instance.GetIntensityFactor(); // 0.0 to 1.0
        bool hasEnergy = LightEnergyManager.Instance.CurrentEnergy > 0;

        // Calculate Target Clip Plane
        float targetClip = deadFarClip;

        if (hasEnergy)
        {
            // Priority: If Flashlight is ON, use max range regardless of energy
            if (flashlight != null && flashlight.IsActive)
            {
                targetClip = flashlightFarClip;
            }
            else
            {
                // Otherwise, scale vision based on remaining energy
                targetClip = Mathf.Lerp(deadFarClip, baseFarClip, energyFactor);
            }
        }

        // Apply to Cinemachine Smoothly
        var lens = virtualCamera.Lens;
        lens.FarClipPlane = Mathf.Lerp(lens.FarClipPlane, targetClip, Time.deltaTime * 2f);
        virtualCamera.Lens = lens;
    }
}