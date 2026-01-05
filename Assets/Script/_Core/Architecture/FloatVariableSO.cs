using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Float Variable")]
public class FloatVariableSO : ScriptableObject
{
    public float Value;

    [TextArea]
    public string Description; // Good for documentation

    // Optional: Reset value on start so data doesn't persist between sessions unexpectedly
    public void SetValue(float value) => Value = value;
}
