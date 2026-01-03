using UnityEngine;

public enum LightSourceType
{
    Flashlight, // Can trigger events, cannot collect souls
    Wisp        // Can trigger events, CAN collect souls
}

public interface ILitObject
{
    void OnLit(LightSourceType sourceType);
    void OnUnlit(LightSourceType sourceType);
}