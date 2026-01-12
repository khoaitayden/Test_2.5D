using UnityEngine;
using System.Collections.Generic;

public class GameObjectiveManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<AreaDefinitionSO> allAreas;
    [SerializeField] private TransformAnchorSO beaconAnchor;
    [SerializeField] private TransformSetSO activeObjectivesSet; 
    [SerializeField] private BoolVariableSO isCarryingItem; 

    [Header("Events")]
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [Header("Scene References")]
    [SerializeField] private PlayerItemCarrier playerCarrier;
    [SerializeField] private List<ArtMapping> ArtMap;

    [System.Serializable]
    public struct ArtMapping {
        public AreaDefinitionSO area;
        public ObjectiveArt art;
    }

    private HashSet<AreaDefinitionSO> completedAreas = new HashSet<AreaDefinitionSO>();
    private AreaDefinitionSO currentHeldAreaItem;
    void Start()
    {
        foreach(var map in ArtMap)
        {
            activeObjectivesSet.Add(map.art.transform);
        }
    }
    void OnEnable()
    {
        objectiveEvents.OnAreaItemPickedUp += HandlePickup;
        objectiveEvents.OnAreaItemPlaced += HandlePlacement;
        
    }
    public void HandleDeath()
    {
        Debug.Log("Objective Manager Handling Death Penalty...");

        // 1. Check Player Hand
        if (currentHeldAreaItem != null)
        {
            // Player died holding an item. Reset that area.
            ResetArea(currentHeldAreaItem);
            
            // Clear Player Hand (Visuals handled by scene reload usually, but if no reload:)
            playerCarrier.SwapItem(null);
            currentHeldAreaItem = null;
        }
        else if (completedAreas.Count > 0)
        {
            // 2. Player empty handed. Punish by taking random completed item.
            // Get random area
            List<AreaDefinitionSO> completedList = new List<AreaDefinitionSO>(completedAreas);
            AreaDefinitionSO victimArea = completedList[Random.Range(0, completedList.Count)];
            
            // Remove from completed
            completedAreas.Remove(victimArea);
            
            // Reset that area
            ResetArea(victimArea);
            
            // TODO: Update Beacon Visuals (Remove the item from the beacon)
            Debug.Log($"Death Penalty: Returned {victimArea.areaName} item to origin.");
        }

    }

    private void HandlePickup(AreaDefinitionSO area)
    {
        currentHeldAreaItem = area;
    }

    private void HandlePlacement(AreaDefinitionSO area)
    {
        currentHeldAreaItem = null;
        completedAreas.Add(area);
        if (isCarryingItem) isCarryingItem.Value = false;
    }

    private void ResetArea(AreaDefinitionSO area)
    {
        // 1. Reset Pedestal (Existing Logic)
        foreach(var map in ArtMap)
        {
            if (map.area == area)
            {
                map.art.ResetArt();
                activeObjectivesSet.Add(map.art.transform);
                break;
            }
        }
        if (objectiveEvents != null)
        {
            objectiveEvents.RaiseAreaReset(area);
        }
    }
}