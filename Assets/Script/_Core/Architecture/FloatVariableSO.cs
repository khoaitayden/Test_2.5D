using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Float Variable")]
public class FloatVariableSO : ScriptableObject
{
    public float Value;
    
    [TextArea] public string Description;

    private void OnDisable() => Value = 0f;

    public void ApplyChange(float amount, float min, float max)
    {
        Value = Mathf.Clamp(Value + amount, min, max);
    }
}