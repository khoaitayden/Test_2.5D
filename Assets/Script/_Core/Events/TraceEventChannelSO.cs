using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Trace Event Channel")]
public class TraceEventChannelSO : ScriptableObject
{
    public UnityAction<Vector3, TraceType> OnTraceEmitted;

    public void RaiseEvent(Vector3 pos, TraceType type)
    {
        if (OnTraceEmitted != null)
        {
            OnTraceEmitted.Invoke(pos, type);
        }
        else
        {
            Debug.LogWarning(" A Trace was emitted, but no one is listening (TraceManager missing?)");
        }
    }
}