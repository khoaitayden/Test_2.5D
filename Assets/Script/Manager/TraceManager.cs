using System.Collections.Generic;
using UnityEngine;

public class TraceManager : MonoBehaviour
{
    // Singleton access is optional now, but useful for Monster AI to query the list later
    public static TraceManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float defaultFootstepDuration = 30f;
    [SerializeField] private float soulTraceDuration = 60f;
    [SerializeField] private int maxTraceCount = 100;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logToConsole = true;

    // The actual memory of traces
    private List<GameTrace> activeTraces = new List<GameTrace>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // SUBSCRIBE to the static bus
        TraceEventBus.OnTraceEmitted += HandleNewTrace;
    }

    private void OnDisable()
    {
        // UNSUBSCRIBE to prevent memory leaks
        TraceEventBus.OnTraceEmitted -= HandleNewTrace;
    }

    // This runs automatically when someone calls TraceEventBus.Emit()
    private void HandleNewTrace(Vector3 pos, TraceType type)
    {
        float duration = defaultFootstepDuration;
        
        // Custom durations based on type
        switch (type)
        {
            case TraceType.Soul_Collection: duration = soulTraceDuration; break;
            case TraceType.Footstep_Run: duration = defaultFootstepDuration; break; 
            case TraceType.Footstep_Walk: duration = defaultFootstepDuration; break;
            case TraceType.Footstep_Jump: duration = defaultFootstepDuration; break;
        }

        GameTrace trace = new GameTrace(pos, type, duration);
        activeTraces.Add(trace);

        // Optimization: Remove oldest if list gets too big
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
        // Remove expired traces (Loop backwards)
        for (int i = activeTraces.Count - 1; i >= 0; i--)
        {
            if (activeTraces[i].IsExpired)
            {
                activeTraces.RemoveAt(i);
            }
        }
    }

    // --- MONSTER AI API ---
    public List<GameTrace> GetTraces()
    {
        return activeTraces;
    }

    // --- VISUALIZATION ---
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        foreach (var trace in activeTraces)
        {
            float ratio = trace.RemainingTime / trace.Duration; // 1.0 (new) to 0.0 (old)
            
            switch (trace.Type)
            {
                case TraceType.Footstep_Run: Gizmos.color = new Color(1, 0, 0, ratio); break; // Red
                case TraceType.Footstep_Walk: Gizmos.color = new Color(1, 0.92f, 0.016f, ratio); break; // Yellow
                case TraceType.Soul_Collection: Gizmos.color = new Color(0, 1, 1, ratio); break; // Cyan
                case TraceType.Footstep_Jump: Gizmos.color = new Color(0, 0, 1, ratio); break; //Blue
                default: Gizmos.color = new Color(1, 1, 1, ratio); break;
            }

            Gizmos.DrawWireSphere(trace.Position, 0.5f);
            Gizmos.DrawLine(trace.Position, trace.Position + Vector3.up * 2f * ratio); // Line shrinks as it fades
        }
    }
}