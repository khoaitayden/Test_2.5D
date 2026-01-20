using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Architecture/Art Registry")]
public class ArtRegistrySO : ScriptableObject
{
    private Dictionary<AreaDefinitionSO, ObjectiveArt> registry = new Dictionary<AreaDefinitionSO, ObjectiveArt>();

    public void Register(AreaDefinitionSO area, ObjectiveArt art)
    {
        if (!registry.ContainsKey(area))
        {
            registry.Add(area, art);
        }
        else
        {
            registry[area] = art;
        }
    }

    public void Unregister(AreaDefinitionSO area)
    {
        if (registry.ContainsKey(area))
        {
            registry.Remove(area);
        }
    }

    public ObjectiveArt GetArt(AreaDefinitionSO area)
    {
        if (registry.ContainsKey(area))
            return registry[area];
        return null;
    }

    private void OnDisable() => registry.Clear();
}