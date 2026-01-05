using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Systems/Trace Storage")]
public class TraceStorageSO : ScriptableObject
{
    // The actual list of active traces
    private List<GameTrace> activeTraces = new List<GameTrace>();

    public void AddTrace(GameTrace trace)
    {
        activeTraces.Add(trace);
    }

    public void RemoveTrace(GameTrace trace)
    {
        if (activeTraces.Contains(trace))
            activeTraces.Remove(trace);
    }
    
    public void RemoveTraceAt(int index)
    {
        if(index >= 0 && index < activeTraces.Count)
            activeTraces.RemoveAt(index);
    }

    public List<GameTrace> GetTraces()
    {
        return activeTraces;
    }

    // Clear list when game starts/stops to prevent data persisting in Editor
    private void OnEnable() => activeTraces.Clear();
    private void OnDisable() => activeTraces.Clear();
}