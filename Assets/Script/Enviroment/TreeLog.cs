using UnityEngine;

public class TreeLog : MonoBehaviour
{
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
        
        // A. SLOW WALK: No Effect, No Trace
        if (player.IsSlowWalking)
        {
            return;
        }

        // B. SPRINTING: Trip + Strong Noise
        if (player.IsSprinting)
        {
            // 1. Freeze Input and Movement (Uses the function we made for Doors/Ladders)
            player.FreezeInteraction(tripDuration);
            
            // 2. Emit Strong Noise
            TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseStrong);
        }

        // C. NORMAL WALK: Slow Down + No Trace
        else
        {
            player.ApplyEnvironmentalSlow(walkSlowFactor, slowDuration);
        }
    }
}