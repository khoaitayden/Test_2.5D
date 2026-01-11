using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))] 
public class ObjectiveItemDisplay : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private MissionItemSO heldItem;

    [Header("References")]
    [SerializeField] private ObjectiveQuest questObjectiveLogic; 

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

    public bool HasItem => heldItem != null;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider>();
        
        if (questObjectiveLogic == null) 
            questObjectiveLogic = GetComponentInParent<ObjectiveQuest>();

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

    // --- MODIFIED INTERACT LOGIC ---
    public bool Interact(GameObject interactor)
    {
        // 1. Is the chest empty?
        if (heldItem == null) return false;

        PlayerItemCarrier carrier = interactor.GetComponent<PlayerItemCarrier>();
        if (carrier != null)
        {
            // 2. NEW CHECK: Are the player's hands full?
            if (carrier.HasItem)
            {
                // Optional: Feedback (Sound effect or UI message)
                Debug.Log("Hands full! Cannot take item.");
                return false; // Reject interaction
            }

            // 3. Take Item
            // Since carrier is empty, SwapItem just gives us null back
            carrier.SwapItem(heldItem);
            heldItem = null; // Chest is now empty
            
            UpdateVisuals();

            // 4. Notify Logic (Remove from Objectives)
            if (questObjectiveLogic != null)
            {
                questObjectiveLogic.OnItemTaken();
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