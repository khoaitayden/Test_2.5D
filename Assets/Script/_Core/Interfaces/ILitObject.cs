using UnityEngine;

public enum LightSourceType
{
    Flashlight, // Triggers events, cannot collect souls
    Wisp        // Triggers events, CAN collect souls
}

public interface ILitObject
{
    void OnLit(LightSourceType sourceType);
    void OnUnlit(LightSourceType sourceType);
}