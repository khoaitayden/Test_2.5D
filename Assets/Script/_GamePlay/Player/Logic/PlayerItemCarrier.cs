using UnityEngine;

public class PlayerItemCarrier : MonoBehaviour
{
    [Header("State Data")]
    [SerializeField] private BoolVariableSO isCarryingItem;
    [Header("Visuals")]
    [Tooltip("The SpriteRenderer on the player's back")]
    [SerializeField] private SpriteRenderer backSpriteRenderer;

    [Header("Current Inventory")]
    [SerializeField] private MissionItemSO currentItem;

    public bool HasItem => currentItem != null;
    public MissionItemSO CurrentItem => currentItem;
    
    // --- NEW HELPER FOR ANIMATION SCRIPT ---
    public SpriteRenderer GetBackSpriteRenderer()
    {
        return backSpriteRenderer;
    }
    // ---------------------------------------

    void Start()
    {
        UpdateVisuals();
    }

    public MissionItemSO SwapItem(MissionItemSO newItem)
    {
        MissionItemSO oldItem = currentItem;
        currentItem = newItem;
        
        UpdateVisuals();
        UpdateState(); // NEW
        
        return oldItem;
    }
    private void UpdateState()
    {
        if (isCarryingItem != null)
        {
            isCarryingItem.Value = (currentItem != null);
        }
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
}