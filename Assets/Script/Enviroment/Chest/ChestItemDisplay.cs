using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))] 
public class ChestItemDisplay : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private MissionItemSO heldItem;

    [Header("Floating Settings")]
    [Tooltip("How high the item moves up when chest opens.")]
    [SerializeField] private float riseHeight = 1.0f;
    [SerializeField] private float riseSpeed = 2f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.1f;

    private Vector3 hiddenPos;
    private Vector3 targetBasePos;
    private SpriteRenderer spriteRenderer;
    private Collider itemCollider;
    private bool isRaised = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider>();
        
        // Assume where we place it in the editor is the "Hidden" (Inside chest) position
        hiddenPos = transform.localPosition;
        targetBasePos = hiddenPos;

        UpdateVisuals();
    }

    void Update()
    {
        // 1. Smoothly move base position (Rising up or Sinking down)
        // We use MoveTowards for linear, smooth movement
        Vector3 currentBasePos = transform.localPosition;
        
        // Remove the bobbing offset from previous frame to get true base calculation
        if (isRaised) currentBasePos.y -= Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        Vector3 nextPos = Vector3.MoveTowards(currentBasePos, targetBasePos, riseSpeed * Time.deltaTime);

        // 2. Add Bobbing ONLY if we are raised (or raising)
        if (isRaised)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            nextPos.y += bobOffset;
        }

        transform.localPosition = nextPos;
        
        // 3. Billboarding (Face Camera)
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    // --- CALLED BY CHEST CONTROLLER ---
    public void SetItemState(bool showItem)
    {
        isRaised = showItem;
        
        if (showItem)
        {
            // Set target to go UP
            targetBasePos = hiddenPos + new Vector3(0, riseHeight, 0);
            if(itemCollider != null) itemCollider.enabled = true; // Enable clicking
        }
        else
        {
            // Set target to go DOWN (Hide)
            targetBasePos = hiddenPos;
            if(itemCollider != null) itemCollider.enabled = false; // Disable clicking
        }
    }

    public bool Interact(GameObject interactor)
    {
        if (heldItem == null) return false;

        PlayerItemCarrier carrier = interactor.GetComponent<PlayerItemCarrier>();
        if (carrier != null)
        {
            MissionItemSO playersOldItem = carrier.SwapItem(heldItem);
            heldItem = playersOldItem;
            UpdateVisuals();
            return true;
        }
        return false;
    }

    private void UpdateVisuals()
    {
        if (heldItem != null)
        {
            spriteRenderer.sprite = heldItem.itemSprite;
            spriteRenderer.enabled = true;
            // Only enable collider if the chest is actually open (isRaised)
            if (isRaised) itemCollider.enabled = true; 
        }
        else
        {
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
            itemCollider.enabled = false;
        }
    }

    public string GetInteractionPrompt()
    {
        if (heldItem == null) return "";
        return $"Take {heldItem.itemName}";
    }
}