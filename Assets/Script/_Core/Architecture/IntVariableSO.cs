using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Int Variable")]
public class IntVariableSO : ScriptableObject
{
    public int Value;
    [TextArea] public string Description;

    public void ApplyChange(int amount)
    {
        Value += amount;
        if (Value < 0) Value = 0; // Safety clamp
    }

    private void OnDisable() => Value = 0; // Reset on stop
}