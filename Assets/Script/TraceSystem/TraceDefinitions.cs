using UnityEngine;

// Expand this list whenever you add new mechanics (dry leaves, doors opening, etc)
public enum TraceType
{
    Footstep_Walk,
    Footstep_Run,
    Soul_Collection,
    Loud_Noise
}

[System.Serializable]
public class GameTrace
{
    public Vector3 Position;
    public TraceType Type;
    public float Timestamp;
    public float Duration;

    public GameTrace(Vector3 pos, TraceType type, float duration)
    {
        Position = pos;
        Type = type;
        Duration = duration;
        Timestamp = Time.time;
    }

    public bool IsExpired => Time.time > (Timestamp + Duration);
    public float RemainingTime => (Timestamp + Duration) - Time.time;
}