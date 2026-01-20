using UnityEngine;

public class TreeLog : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel;

    [Header("Trip Settings (Sprinting)")]
    [SerializeField] private float tripDuration = 0.5f; 

    [Header("Slow Settings (Walking)")]
    [SerializeField] private float walkSlowFactor = 0.6f;
    [SerializeField] private float slowDuration = 2.0f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController controller = other.GetComponent<PlayerController>();
        PlayerMovement movement = other.GetComponent<PlayerMovement>();

        if (controller == null || movement == null) return;

        bool isSneaking = InputManager.Instance.IsSlowWalking;
        bool isSprinting = InputManager.Instance.IsSprinting;

        if (isSneaking)
        {
            return;
        }

        if (isSprinting)
        {
            // 1. Freeze Input via Controller
            controller.FreezeInteraction(tripDuration);
            
            // 2. Emit Strong Noise
            if (traceChannel != null) traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseStrong);
        }
        else
        {
            // Apply Slow via Movement
            movement.ApplyEnvironmentalSlow(walkSlowFactor, slowDuration);
            if (traceChannel != null) traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
        }
    }
}