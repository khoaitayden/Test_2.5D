using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))] 
public class ChestItemDisplay : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private MissionItemSO heldItem;

    [Header("References")]
    // NEW: Reference to the logic script
    [SerializeField] private ChestQuest questChestLogic; 

    // ... (Keep your Floating/Visual Settings here) ...
    [Header("Floating Settings")]
    [SerializeField] private float riseHeight = 1.0f;
    [SerializeField] private float riseSpeed = 2f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.1f;

    private Vector3 hiddenPos;
    private Vector3 targetBasePos;
    private SpriteRenderer spriteRenderer;
    private Collider itemCollider;
    private bool isRaised = false;

    // Helper property for QuestChest to check on Start
    public bool HasItem => heldItem != null;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider>();

        hiddenPos = transform.localPosition;
        targetBasePos = hiddenPos;

        UpdateVisuals();
    }

    void Update()
    {
        Vector3 currentBasePos = transform.localPosition;
        if (isRaised) currentBasePos.y -= Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        Vector3 nextPos = Vector3.MoveTowards(currentBasePos, targetBasePos, riseSpeed * Time.deltaTime);
        if (isRaised) nextPos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = nextPos;
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
    }

    public void SetItemState(bool showItem)
    {
        isRaised = showItem;
        targetBasePos = showItem ? hiddenPos + new Vector3(0, riseHeight, 0) : hiddenPos;
        if (itemCollider != null) itemCollider.enabled = showItem;
    }

    // --- UPDATED INTERACT LOGIC ---
    public bool Interact(GameObject interactor)
    {
        if (heldItem == null) return false;

        PlayerItemCarrier carrier = interactor.GetComponent<PlayerItemCarrier>();
        if (carrier != null)
        {
            MissionItemSO playersOldItem = carrier.SwapItem(heldItem);
            heldItem = playersOldItem; // Usually null if player hand was empty
            
            UpdateVisuals();

            // NEW: Check if the chest is now empty
            if (heldItem == null)
            {
                // Notify the logic script
                if (questChestLogic != null)
                {
                    questChestLogic.OnItemTaken();
                }
            }

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