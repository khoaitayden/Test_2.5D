using UnityEngine;

public class BeaconController : MonoBehaviour, IInteractable
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
    [Header("Events")]
    [SerializeField] private GameEventSO unlockEyeEvent;
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