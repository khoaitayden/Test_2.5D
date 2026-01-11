using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Bool Variable")]
public class BoolVariableSO : ScriptableObject
{
    public bool Value;
    
    [TextArea] public string Description;
    private void OnDisable() => Value = false;
}