using UnityEngine;

public class ObjectiveArt : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private AreaDefinitionSO areaData; 
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;
    
    [Header("Architecture")]
    [SerializeField] private ArtRegistrySO artRegistry; // Drag "registry_ObjectiveArts"
    [SerializeField] private TransformSetSO activeObjectivesSet; // Drag "set_ActiveObjectives"

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private GameObject visualRoot;

    private bool hasItem = true;

    // --- REGISTRATION LOGIC ---
    void OnEnable()
    {
        // 1. Register self so Manager can find me
        if (artRegistry != null) artRegistry.Register(areaData, this);

        // 2. Tell Wisp I exist (if I have the item)
        if (hasItem && activeObjectivesSet != null)
        {
            activeObjectivesSet.Add(this.transform);
        }
    }

    void OnDisable()
    {
        if (artRegistry != null) artRegistry.Unregister(areaData);
        if (activeObjectivesSet != null) activeObjectivesSet.Remove(this.transform);
    }
    // ---------------------------

    void Start()
    {
        UpdateVisuals();
    }

    public void ResetArt()
    {
        hasItem = true;
        UpdateVisuals();
        
        // Add back to Wisp
        if (activeObjectivesSet != null) activeObjectivesSet.Add(this.transform);
    }

    public bool Interact(GameObject interactor)
    {
        if (!hasItem) return false;

        PlayerItemCarrier carrier = interactor.GetComponent<PlayerItemCarrier>();
        
        if (carrier != null && !carrier.HasItem)
        {
            carrier.SwapItem(areaData.associatedItem);
            hasItem = false;
            UpdateVisuals();

            // Remove from Wisp
            if (activeObjectivesSet != null) activeObjectivesSet.Remove(this.transform);

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