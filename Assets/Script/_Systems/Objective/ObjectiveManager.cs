using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Data Monitoring")]
    [SerializeField] private TransformSetSO activeChestsSet;
    [SerializeField] private BoolVariableSO isCarryingItem;
    
    // Optional: Event when all chests are done?
    [SerializeField] private GameEventSO allObjectivesCompleteEvent; 

    private bool allCompleteTriggered = false;

    void Update()
    {
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (activeChestsSet == null) return;

        // If no chests left AND player is not holding anything
        if (activeChestsSet.GetItems().Count == 0 && !isCarryingItem.Value)
        {
            if (!allCompleteTriggered)
            {
                Debug.Log("ALL OBJECTIVES COMPLETE!");
                if (allObjectivesCompleteEvent != null) allObjectivesCompleteEvent.Raise();
                allCompleteTriggered = true;
            }
        }
    }
}