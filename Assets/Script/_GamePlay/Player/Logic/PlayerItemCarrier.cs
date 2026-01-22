using UnityEngine;

public class PlayerItemCarrier : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("The SpriteRenderer on the player's back")]
    [SerializeField] private SpriteRenderer backSpriteRenderer;

    [Header("Current Inventory")]
    [SerializeField] private MissionItemSO currentItem;

    [Header("State Data")]
    [SerializeField] private BoolVariableSO isCarryingItem;

    public bool HasItem => currentItem != null;
    public MissionItemSO CurrentItem => currentItem;
    
    public SpriteRenderer GetBackSpriteRenderer()
    {
        return backSpriteRenderer;
    }

    void Start()
    {
        // RESET ON START
        currentItem = null; // Clear internal item
        UpdateVisuals();
        UpdateState(); 
    }

    public MissionItemSO SwapItem(MissionItemSO newItem)
    {
        MissionItemSO oldItem = currentItem;
        currentItem = newItem;
        UpdateVisuals();
        UpdateState();
        return oldItem;
    }

    private void UpdateVisuals()
    {
        if (currentItem != null)
        {
            backSpriteRenderer.sprite = currentItem.itemSprite;
            backSpriteRenderer.enabled = true;
        }
        else
        {
            backSpriteRenderer.sprite = null;
            backSpriteRenderer.enabled = false;
        }
    }

    private void UpdateState()
    {
        if (isCarryingItem != null)
        {
            isCarryingItem.Value = (currentItem != null);
        }
    }
}