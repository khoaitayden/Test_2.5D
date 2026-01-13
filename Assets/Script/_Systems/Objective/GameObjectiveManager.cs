using UnityEngine;
using System.Collections.Generic;

public class GameObjectiveManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BoolVariableSO isCarryingItem;

    [Header("Events")]
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [Header("Scene References")]
    [SerializeField] private PlayerItemCarrier playerCarrier;

    private HashSet<AreaDefinitionSO> completedAreas = new HashSet<AreaDefinitionSO>();
    private AreaDefinitionSO currentHeldAreaItem;
    void OnEnable()
    {
        objectiveEvents.OnAreaItemPickedUp += HandlePickup;
        objectiveEvents.OnAreaItemPlaced += HandlePlacement;
    }
    
    // Don't forget OnDisable!
    void OnDisable()
    {
        objectiveEvents.OnAreaItemPickedUp -= HandlePickup;
        objectiveEvents.OnAreaItemPlaced -= HandlePlacement;
    }
    
     public void HandleDeath()
    {
        Debug.Log("Objective Manager Handling Death Penalty...");

        if (currentHeldAreaItem != null)
        {
            ResetArea(currentHeldAreaItem);
            playerCarrier.SwapItem(null);
            currentHeldAreaItem = null;
        }
        else if (completedAreas.Count > 0)
        {
            List<AreaDefinitionSO> completedList = new List<AreaDefinitionSO>(completedAreas);
            AreaDefinitionSO victimArea = completedList[Random.Range(0, completedList.Count)];
            
            completedAreas.Remove(victimArea);
            ResetArea(victimArea);
            
            Debug.Log($"Death Penalty: Returned {victimArea.areaName} item to origin.");
        }
    }

    private void HandlePickup(AreaDefinitionSO area)
    {
        currentHeldAreaItem = area;
        // isCarryingItem handled by PlayerItemCarrier script usually, 
        // but if you want to force it:
        if (isCarryingItem) isCarryingItem.Value = true;
    }

    private void HandlePlacement(AreaDefinitionSO area)
    {
        currentHeldAreaItem = null;
        completedAreas.Add(area);
        if (isCarryingItem) isCarryingItem.Value = false;
    }

    private void ResetArea(AreaDefinitionSO area)
    {
        if (objectiveEvents != null)
        {
            objectiveEvents.RaiseAreaReset(area);
        }
    }
}