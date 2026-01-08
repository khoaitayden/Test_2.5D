using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Systems/Trace Storage")]
public class TraceStorageSO : ScriptableObject
{
    private List<GameTrace> activeTraces = new List<GameTrace>();

    public void AddTrace(GameTrace trace) => activeTraces.Add(trace);

    public void RemoveTraceAt(int index)
    {
        if (index >= 0 && index < activeTraces.Count)
            activeTraces.RemoveAt(index);
    }

    public List<GameTrace> GetTraces() => activeTraces;

    // --- NEW: Public Clear Method ---
    public void ClearAll()
    {
        activeTraces.Clear();
    }

    // Safety for Editor resets
    private void OnDisable() => ClearAll();
    private void OnEnable() => ClearAll();
}