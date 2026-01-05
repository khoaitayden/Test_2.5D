using UnityEngine;

public class TreeBranch : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    [Header("Settings")]
    [Tooltip("How long the slow lasts (in seconds).")]
    [SerializeField] private float slowDuration = 2.0f;

    [Header("Penalties")]
    [Tooltip("Multiplier when Walking (e.g. 0.75 for 25% slow)")]
    [SerializeField] private float walkSlowMultiplier = 0.75f;
    
    [Tooltip("Multiplier when Sprinting (e.g. 0.50 for 50% slow)")]
    [SerializeField] private float sprintSlowMultiplier = 0.50f;

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            // --- 1. SLOW WALK (Sneaking) ---
            // Effect: No Slow, Weak Noise
            if (player.IsSlowWalking)
            {
                traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseWeak);
            }
            
            // --- 2. SPRINTING (Running) ---
            // Effect: Heavy Slow (50%), Strong Noise
            else if (player.IsSprinting)
            {
                player.ApplyEnvironmentalSlow(sprintSlowMultiplier, slowDuration);
                
                traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseStrong);
            }
            
            // --- 3. NORMAL WALK ---
            // Effect: Medium Slow (25%), Medium Noise
            else 
            {
                player.ApplyEnvironmentalSlow(walkSlowMultiplier, slowDuration);
                
                traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium);
            }
        }
    }
}