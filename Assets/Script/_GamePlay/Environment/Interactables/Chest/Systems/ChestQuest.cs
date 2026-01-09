using UnityEngine;

public class ChestQuest : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TransformSetSO activeChestsSet; 

    [Header("References")]
    [SerializeField] private ChestItemDisplay itemDisplay;

    private bool isRegistered = false;

    void Start()
    {
        // Only register if there is actually an item inside at the start
        if (itemDisplay != null && itemDisplay.HasItem)
        {
            RegisterChest();
        }
    }

    void OnDisable()
    {
        // Safety: ensure we are removed if the object is disabled/destroyed
        UnregisterChest();
    }

    // --- PUBLIC METHOD called by ChestItemDisplay ---
    public void OnItemTaken()
    {
        UnregisterChest();
        Debug.Log($"[QuestChest] Item taken from {name}. Removed from objectives.");
    }

    private void RegisterChest()
    {
        if (!isRegistered && activeChestsSet != null)
        {
            activeChestsSet.Add(this.transform);
            isRegistered = true;
        }
    }

    private void UnregisterChest()
    {
        if (isRegistered && activeChestsSet != null)
        {
            activeChestsSet.Remove(this.transform);
            isRegistered = false;
        }
    }
}