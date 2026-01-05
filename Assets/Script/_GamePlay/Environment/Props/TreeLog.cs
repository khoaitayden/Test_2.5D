using UnityEngine;

public class TreeLog : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    [Header("Trip Settings (Sprinting)")]
    [Tooltip("How long the player cannot move after tripping.")]
    [SerializeField] private float tripDuration = 0.5f; 

    [Header("Slow Settings (Walking)")]
    [Tooltip("Speed multiplier when walking over (e.g., 0.5 is half speed).")]
    [SerializeField] private float walkSlowFactor = 0.6f;
    [SerializeField] private float slowDuration = 2.0f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // Check 2: State Logic
        
        if (player.IsSlowWalking)
        {
            return;
        }

        if (player.IsSprinting)
        {
            // 1. Freeze Input and Movement (Uses the function we made for Doors/Ladders)
            player.FreezeInteraction(tripDuration);
            
            // 2. Emit Strong Noise
            traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseStrong);
        }

        else
        {
            player.ApplyEnvironmentalSlow(walkSlowFactor, slowDuration);
            traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
        }
    }
}