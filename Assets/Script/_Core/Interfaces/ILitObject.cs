using UnityEngine;

public enum LightSourceType
{
    Flashlight,
    Wisp        
}

public interface ILitObject
{
    void OnLit(LightSourceType sourceType);
    void OnUnlit(LightSourceType sourceType);
}