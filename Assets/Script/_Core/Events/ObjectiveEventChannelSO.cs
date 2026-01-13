using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Objective Event Channel")]
public class ObjectiveEventChannelSO : ScriptableObject
{
    public UnityAction<AreaDefinitionSO> OnAreaItemPickedUp;
    public UnityAction<AreaDefinitionSO> OnAreaItemPlaced;
    
    // NEW: Logic for Death Penalty / Reset
    public UnityAction<AreaDefinitionSO> OnAreaReset; 

    public void RaiseItemPickedUp(AreaDefinitionSO area) => OnAreaItemPickedUp?.Invoke(area);
    public void RaiseItemPlaced(AreaDefinitionSO area) => OnAreaItemPlaced?.Invoke(area);
    
    // NEW
    public void RaiseAreaReset(AreaDefinitionSO area) => OnAreaReset?.Invoke(area);
}