using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Architecture/Transform Set")]
public class TransformSetSO : ScriptableObject
{
    private List<Transform> items = new List<Transform>();

    public void Add(Transform t)
    {
        if (!items.Contains(t)) items.Add(t);
    }

    public void Remove(Transform t)
    {
        if (items.Contains(t)) items.Remove(t);
    }

    public List<Transform> GetItems() => items;

    private void OnDisable() => items.Clear();
}