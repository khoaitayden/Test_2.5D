using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer playerBodyRenderer;
    [SerializeField] private PlayerItemCarrier itemCarrier;     
    
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerClimbing playerClimbing;
    [SerializeField] private Transform playerRoot; // The parent object (Player)

    private Animator animator;
    private Transform mainCameraTransform;

    private readonly int animHorizontal = Animator.StringToHash("HorizontalInput");
    private readonly int animVertical = Animator.StringToHash("VerticalInput");
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animIsClimbing = Animator.StringToHash("IsClimbing");

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCameraTransform = Camera.main.transform;

        // Auto-find references if not assigned
        if (playerBodyRenderer == null) playerBodyRenderer = GetComponent<SpriteRenderer>();
        if (itemCarrier == null) itemCarrier = GetComponentInParent<PlayerItemCarrier>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerClimbing == null) playerClimbing = GetComponentInParent<PlayerClimbing>();
        if (playerRoot == null) playerRoot = transform.parent;
    }

    void LateUpdate()
    {
        // 1. BILLBOARDING
        Vector3 lookPos = mainCameraTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        float verticalInput = 0f;
        float horizontalInput = 0f;
        bool isClimbing = playerClimbing != null && (playerClimbing.IsClimbing || playerClimbing.IsEnteringLadder);

        // 2. CALCULATE INPUTS
        if (isClimbing)
        {
            // Climbing is always "Back View"
            verticalInput = 1.0f;
            horizontalInput = 0.0f;
        }
        else
        {
            Vector3 playerForward = playerRoot.forward;
            Vector3 cameraDirection = playerRoot.position - mainCameraTransform.position;
            cameraDirection.y = 0;
            cameraDirection.Normalize();

            verticalInput = Vector3.Dot(cameraDirection, playerForward);
            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
            horizontalInput = Vector3.Dot(cameraDirection, playerRight);
        }

        // 3. APPLY TO ANIMATOR
        animator.SetFloat(animVertical, verticalInput);
        animator.SetFloat(animHorizontal, horizontalInput);
        
        // Get speed from PlayerMovement component
        float speed = playerMovement != null ? playerMovement.CurrentHorizontalSpeed : 0f;
        animator.SetFloat(animSpeed, speed);
        animator.SetBool(animIsClimbing, isClimbing);

        // 4. HANDLE ITEM SORTING
        UpdateItemSorting(verticalInput);
    }

    private void UpdateItemSorting(float verticalVal)
    {
        if (itemCarrier == null || !itemCarrier.HasItem) return;

        SpriteRenderer itemRenderer = itemCarrier.GetBackSpriteRenderer();
        if (itemRenderer == null) return;

        // verticalVal > 0.1 means facing AWAY from camera (Back View) -> Item on TOP
        // verticalVal < 0.1 means facing TOWARDS camera (Front/Side View) -> Item BEHIND
        if (verticalVal > 0.1f) 
        {
            itemRenderer.sortingOrder = playerBodyRenderer.sortingOrder + 1;
        }
        else
        {
            itemRenderer.sortingOrder = playerBodyRenderer.sortingOrder - 1;
        }
    }

    // --- PUBLIC API (Linked via GameEventListener in Inspector) ---
    public void Jump() { animator.SetTrigger("Jump"); }
    public void Land() { animator.SetTrigger("Land"); }
}