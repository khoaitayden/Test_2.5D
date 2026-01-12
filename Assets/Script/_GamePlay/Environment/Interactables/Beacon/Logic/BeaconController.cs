using UnityEngine;

public class BeaconController : MonoBehaviour, IInteractable
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    [Header("Events")]
    [SerializeField] private GameEventSO unlockEyeEvent;
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;
    [Header("Visuals")]
    [Tooltip("The parent object holding the layers (the one that rotates/floats).")]
    [SerializeField] private GameObject displayRoot;
    
    [Tooltip("The 4 SpriteRenderers representing the layers of the picture.")]
    [SerializeField] private SpriteRenderer[] pictureLayers;

    // Track which layers are filled
    private bool[] filledLayers; 

    void Awake()
    {
        // Initialize state
        int layerCount = pictureLayers.Length; // Should be 4
        filledLayers = new bool[layerCount];

        // Hide all layers initially
        foreach (var layer in pictureLayers)
        {
            if(layer != null) layer.enabled = false;
        }
    }
    void OnEnable()
    {
        if (objectiveEvents != null)
            objectiveEvents.OnAreaReset += HandleAreaReset;
    }

    void OnDisable()
    {
        if (objectiveEvents != null)
            objectiveEvents.OnAreaReset -= HandleAreaReset;
    }
    private void HandleAreaReset(AreaDefinitionSO area)
    {
        if (area == null || area.associatedItem == null) return;

        // Find which layer corresponds to this item
        int index = area.associatedItem.puzzleLayerIndex;

        if (index >= 0 && index < pictureLayers.Length)
        {
            // Turn off the visual
            if (pictureLayers[index] != null)
            {
                pictureLayers[index].enabled = false;
            }

            // Mark as empty so player can place it again
            filledLayers[index] = false;
            
            Debug.Log($"[Beacon] Removed visual for {area.areaName} due to penalty.");
        }
    }
    public bool Interact(GameObject interactor)
    {
        // 1. Get Player Carrier
        PlayerItemCarrier carrier = interactor.GetComponent<PlayerItemCarrier>();
        if (carrier == null || !carrier.HasItem) return false;

        MissionItemSO item = carrier.CurrentItem;

        // 3. Check if this specific layer is already filled
        int index = item.puzzleLayerIndex;
        if (index < 0 || index >= filledLayers.Length) return false;

        if (filledLayers[index]) 
        {
            Debug.Log("This part of the picture is already done.");
            return false;
        }

        // 4. PLACE ITEM
        // Remove item from player (Swap with null)
        carrier.SwapItem(null);

        // Turn on the visual layer
        if (pictureLayers[index] != null)
        {
            pictureLayers[index].sprite = item.itemSprite;
            pictureLayers[index].enabled = true;
        }
        
        // Mark as filled
        filledLayers[index] = true;
        unlockEyeEvent.Raise();
        // Emit Trace/Sound
        traceChannel.RaiseEvent(transform.position, TraceType.EnviromentNoiseStrong);
        Debug.Log($"Placed Layer {index}");

        // Check for Win Condition?
        CheckCompletion();

        return true;
    }

    private void CheckCompletion()
    {
        foreach (bool isFilled in filledLayers)
        {
            if (!isFilled) return;
        }

        Debug.Log("PUZZLE COMPLETED!");
    }

    public string GetInteractionPrompt()
    {
        return "Place Fragment";
    }
}