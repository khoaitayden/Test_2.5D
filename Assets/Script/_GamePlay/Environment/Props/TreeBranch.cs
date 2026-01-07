using UnityEngine;

public class TreeBranch : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; // Use channel instead of static bus if possible

    [Header("Settings")]
    [SerializeField] private float slowDuration = 2.0f;
    [SerializeField] private float walkSlowMultiplier = 0.75f;
    [SerializeField] private float sprintSlowMultiplier = 0.50f;

    void OnTriggerEnter(Collider other)
    {
        // We need movement capability for the slow
        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        
        if (movement != null)
        {
            // Read input directly from Manager (fastest fix) or inject SOs
            bool isSneaking = InputManager.Instance.IsSlowWalking;
            bool isSprinting = InputManager.Instance.IsSprinting;

            // --- 1. SNEAKING ---
            if (isSneaking) 
            {
                // TraceEventBus.Emit(...) -> Use your channel here if you want full decoupling
                // For now, keeping static bus call to match your current Trace setup if you haven't fully switched yet
                // Or better:
                if (traceChannel != null) traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseWeak);
            }
            
            // --- 2. SPRINTING ---
            else if (isSprinting)
            {
                // Call ApplyEnvironmentalSlow on the MOVEMENT component, not controller
                movement.ApplyEnvironmentalSlow(sprintSlowMultiplier, slowDuration);
                
                if (traceChannel != null) traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseStrong);
            }
            
            // --- 3. NORMAL WALK ---
            else 
            {
                movement.ApplyEnvironmentalSlow(walkSlowMultiplier, slowDuration);
                
                if (traceChannel != null) traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseMedium); 
            }
        }
    }
}