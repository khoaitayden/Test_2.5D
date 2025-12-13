using System.Collections.Generic;
using UnityEngine;

public class TraceManager : MonoBehaviour
{
    public static TraceManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float footstepDuration = 25f;
    [SerializeField] private float soulTraceDuration = 40f;
    [SerializeField] private float enviromentNoiseDuration = 20f; // Duration for branches, doors, etc.
    [SerializeField] private int maxTraceCount = 100;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logToConsole = true;

    private List<GameTrace> activeTraces = new List<GameTrace>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable() => TraceEventBus.OnTraceEmitted += HandleNewTrace;
    private void OnDisable() => TraceEventBus.OnTraceEmitted -= HandleNewTrace;

    private void HandleNewTrace(Vector3 pos, TraceType type)
    {
        float duration = footstepDuration;
        
        // Assign duration based on type
        switch (type)
        {
            case TraceType.Soul_Collection: 
                duration = soulTraceDuration; 
                break;
            case TraceType.Footstep_Run: 
                duration = footstepDuration*1.5f; 
                break;
            case TraceType.Footstep_Walk: 
                duration = footstepDuration*1; 
                break;
            
            case TraceType.EnviromentNoiseWeak:
                duration = enviromentNoiseDuration*1; 
                break;
            case TraceType.EnviromentNoiseMedium:
                duration = enviromentNoiseDuration*1.5f; 
                break;
            case TraceType.EnviromentNoiseStrong:
                duration = enviromentNoiseDuration*2f; 
                break;
        }

        GameTrace trace = new GameTrace(pos, type, duration);
        activeTraces.Add(trace);

        if (activeTraces.Count > maxTraceCount)
        {
            activeTraces.RemoveAt(0);
        }

        if (logToConsole)
        {
            Debug.Log($"<color=cyan>[Trace]</color> Recorded: {type} at {pos}");
        }
    }

    private void Update()
    {
        for (int i = activeTraces.Count - 1; i >= 0; i--)
        {
            if (activeTraces[i].IsExpired) activeTraces.RemoveAt(i);
        }
    }

    public List<GameTrace> GetTraces() => activeTraces;

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        foreach (var trace in activeTraces)
        {
            float ratio = trace.RemainingTime / trace.Duration;
            
            // Set Color based on Type
            switch (trace.Type)
            {
                // Player Movements
                case TraceType.Footstep_Run:    Gizmos.color = new Color(1f, 0f, 0f, ratio); break; // Red
                case TraceType.Footstep_Walk:   Gizmos.color = new Color(1f, 0.92f, 0.016f, ratio); break; // Yellow
                case TraceType.Soul_Collection: Gizmos.color = new Color(0f, 1f, 1f, ratio); break; // Cyan

                // NEW: Environment Noises
                case TraceType.EnviromentNoiseWeak:   
                    Gizmos.color = new Color(0.5f, 1f, 0.5f, ratio); break; // Light Green (Subtle)
                case TraceType.EnviromentNoiseMedium: 
                    Gizmos.color = new Color(1f, 0.5f, 0f, ratio); break; // Orange (Noticeable)
                case TraceType.EnviromentNoiseStrong: 
                    Gizmos.color = new Color(0.5f, 0f, 0.5f, ratio); break; // Dark Purple (Loud/Dangerous)
                
                default: Gizmos.color = new Color(1f, 1f, 1f, ratio); break;
            }

            Gizmos.DrawWireSphere(trace.Position, 0.5f);
            Gizmos.DrawLine(trace.Position, trace.Position + Vector3.up * 2f * ratio);
        }
    }
}