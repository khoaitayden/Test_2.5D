using System;
using UnityEngine;

public static class TraceEventBus
{
    // The event that Manager listens to
    public static event Action<Vector3, TraceType> OnTraceEmitted;

    /// <summary>
    /// Call this from ANY script to drop a trace.
    /// </summary>
    public static void Emit(Vector3 position, TraceType type)
    {
        // Invoke the event safely. If no Manager exists, nothing happens (no errors).
        OnTraceEmitted?.Invoke(position, type);
    }
}