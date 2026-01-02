using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer playerBodyRenderer; // Assign Player Sprite Here
    [SerializeField] private PlayerItemCarrier itemCarrier;     // Assign Player script Here

    private Animator animator;
    private Transform mainCameraTransform;
    private Transform playerParentTransform;
    private PlayerController playerController;

    private readonly int animHorizontal = Animator.StringToHash("HorizontalInput");
    private readonly int animVertical = Animator.StringToHash("VerticalInput");
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animIsClimbing = Animator.StringToHash("IsClimbing");

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerParentTransform = playerController.transform;
        animator = GetComponent<Animator>();
        mainCameraTransform = Camera.main.transform;

        // Auto-find references if not assigned
        if (playerBodyRenderer == null) playerBodyRenderer = GetComponent<SpriteRenderer>();
        if (itemCarrier == null) itemCarrier = GetComponentInParent<PlayerItemCarrier>();
    }

    void LateUpdate()
    {
        // 1. BILLBOARDING
        Vector3 lookPos = mainCameraTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        float verticalInput = 0f;
        float horizontalInput = 0f;
        bool isClimbing = playerController.IsClimbing || playerController.IsEnteringLadder;

        // 2. CALCULATE INPUTS
        if (isClimbing)
        {
            // Climbing is always "Back View"
            verticalInput = 1.0f;
            horizontalInput = 0.0f;
        }
        else
        {
            Vector3 playerForward = playerParentTransform.forward;
            Vector3 cameraDirection = playerParentTransform.position - mainCameraTransform.position;
            cameraDirection.y = 0;
            cameraDirection.Normalize();

            verticalInput = Vector3.Dot(cameraDirection, playerForward);
            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
            horizontalInput = Vector3.Dot(cameraDirection, playerRight);
        }

        // 3. APPLY TO ANIMATOR
        animator.SetFloat(animVertical, verticalInput);
        animator.SetFloat(animHorizontal, horizontalInput);
        animator.SetFloat(animSpeed, playerController.WorldSpaceMoveDirection.magnitude);

        // --- 4. HANDLE ITEM SORTING (THE FIX) ---
        UpdateItemSorting(verticalInput);
    }

    private void UpdateItemSorting(float verticalVal)
    {
        // If we don't have an item or references are missing, stop.
        if (itemCarrier == null || !itemCarrier.HasItem) return;

        SpriteRenderer itemRenderer = itemCarrier.GetBackSpriteRenderer();
        if (itemRenderer == null) return;

        // LOGIC:
        // verticalVal > 0.1 means the player is facing AWAY from camera (Back View).
        // Item should be visible ON TOP of the player.
        
        // verticalVal < 0.1 means player is facing TOWARDS camera (Front/Side View).
        // Item should be hidden BEHIND the player.

        if (verticalVal > 0.1f) 
        {
            // Show Item (Higher order than body)
            itemRenderer.sortingOrder = playerBodyRenderer.sortingOrder + 1;
        }
        else
        {
            // Hide Item (Lower order than body)
            itemRenderer.sortingOrder = playerBodyRenderer.sortingOrder - 1;
        }
    }

    public void Jump() { animator.SetTrigger("Jump"); }
    public void Land() { animator.SetTrigger("Land"); }
}