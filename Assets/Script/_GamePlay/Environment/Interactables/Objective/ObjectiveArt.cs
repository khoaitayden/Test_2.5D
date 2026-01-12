using UnityEngine;

public class ObjectiveArt : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private AreaDefinitionSO areaData; // Define which area this is
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private GameObject visualRoot;

    private bool hasItem = true;

    void Start()
    {
        UpdateVisuals();
    }

    public void ResetArt()
    {
        hasItem = true;
        UpdateVisuals();
    }

    public bool Interact(GameObject interactor)
    {
        if (!hasItem) return false;

        PlayerItemCarrier carrier = interactor.GetComponent<PlayerItemCarrier>();
        
        // 1. Logic: Can only pick up if hands are empty
        if (carrier != null && !carrier.HasItem)
        {
            // Give item to player
            carrier.SwapItem(areaData.associatedItem);
            
            // Remove from pedestal
            hasItem = false;
            UpdateVisuals();

            // 2. FIRE EVENT -> Spawns Monster
            if (objectiveEvents != null)
                objectiveEvents.RaiseItemPickedUp(areaData);

            return true;
        }

        return false;
    }

    private void UpdateVisuals()
    {
        if (visualRoot) visualRoot.SetActive(hasItem);
        if (hasItem && itemRenderer && areaData.associatedItem)
        {
            itemRenderer.sprite = areaData.associatedItem.itemSprite;
        }
    }

    public string GetInteractionPrompt()
    {
        return hasItem ? $"Take {areaData.associatedItem.itemName}" : "";
    }
}