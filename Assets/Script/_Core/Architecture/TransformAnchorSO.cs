using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Architecture/Transform Anchor")]
public class TransformAnchorSO : ScriptableObject
{
    [Tooltip("The transform currently registered to this anchor")]
    public Transform Value;
    public event UnityAction OnAnchorProvided;
    public event UnityAction OnAnchorRemoved;

    public void Provide(Transform transform)
    {
        Value = transform;
        OnAnchorProvided?.Invoke();
    }

    public void Unset()
    {
        Value = null;
        OnAnchorRemoved?.Invoke();
    }

    private void OnDisable()
    {
        Value = null;
    }
}