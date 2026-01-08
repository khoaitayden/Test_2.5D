using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Bool Variable")]
public class BoolVariableSO : ScriptableObject
{
    public bool Value;
    
    [TextArea] public string Description;

    // Reset on disable so the game doesn't start with "Sprinting = true" from last session
    private void OnDisable() => Value = false;
}