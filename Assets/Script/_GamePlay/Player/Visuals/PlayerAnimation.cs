using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer playerBodyRenderer;
    [SerializeField] private PlayerItemCarrier itemCarrier;     
    
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerClimbing playerClimbing;
    [SerializeField] private Transform playerRoot;

    private Animator animator;
    private Transform mainCameraTransform;

    // Animator Hashes
    private readonly int animHorizontal = Animator.StringToHash("HorizontalInput");
    private readonly int animVertical = Animator.StringToHash("VerticalInput");
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animIsClimbing = Animator.StringToHash("IsClimbing");

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCameraTransform = Camera.main.transform;

        // Auto-find references if missing
        if (playerBodyRenderer == null) playerBodyRenderer = GetComponent<SpriteRenderer>();
        if (itemCarrier == null) itemCarrier = GetComponentInParent<PlayerItemCarrier>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerClimbing == null) playerClimbing = GetComponentInParent<PlayerClimbing>();
        if (playerRoot == null) playerRoot = transform.parent;
    }

    void LateUpdate()
    {
        // 1. Rotate sprite to face camera
        HandleBillboarding();

        // 2. Determine animation states
        bool isClimbing = IsPlayerClimbing();
        Vector2 animInput = CalculateAnimationDirection(isClimbing);

        // 3. Send data to Animator
        UpdateAnimatorParameters(animInput, isClimbing);

        // 4. Sort carried items (Behind or In Front of player)
        UpdateItemSorting(animInput.y);
    }

    // --- HELPER METHODS ---

    private void HandleBillboarding()
    {
        if (mainCameraTransform == null) return;

        Vector3 lookPos = mainCameraTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    private bool IsPlayerClimbing()
    {
        return playerClimbing != null && (playerClimbing.IsClimbing || playerClimbing.IsEnteringLadder);
    }

    private Vector2 CalculateAnimationDirection(bool isClimbing)
    {
        // CASE A: Climbing (Always show Back Sprite)
        if (isClimbing)
        {
            return new Vector2(0f, 1.0f); // x=Horizontal, y=Vertical
        }

        // CASE B: Normal Movement (Calculate relative to Camera)
        Vector3 playerForward = playerRoot.forward;
        Vector3 cameraDirection = playerRoot.position - mainCameraTransform.position;
        cameraDirection.y = 0;
        cameraDirection.Normalize();

        float vertical = Vector3.Dot(cameraDirection, playerForward);
        
        Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
        float horizontal = Vector3.Dot(cameraDirection, playerRight);

        return new Vector2(horizontal, vertical);
    }

    private void UpdateAnimatorParameters(Vector2 input, bool isClimbing)
    {
        animator.SetFloat(animVertical, input.y);
        animator.SetFloat(animHorizontal, input.x);
        animator.SetBool(animIsClimbing, isClimbing);

        float speed = (playerMovement != null) ? playerMovement.CurrentHorizontalSpeed : 0f;
        animator.SetFloat(animSpeed, speed);
    }

    private void UpdateItemSorting(float verticalVal)
    {
        if (itemCarrier == null || !itemCarrier.HasItem) return;

        SpriteRenderer itemRenderer = itemCarrier.GetBackSpriteRenderer();
        if (itemRenderer == null) return;

        // Vertical > 0.1 means facing AWAY from camera (Back view) -> Item on top
        // Vertical < 0.1 means facing TOWARDS camera (Front view) -> Item behind
        if (verticalVal > 0.1f) 
        {
            itemRenderer.sortingOrder = playerBodyRenderer.sortingOrder + 1;
        }
        else
        {
            itemRenderer.sortingOrder = playerBodyRenderer.sortingOrder - 1;
        }
    }
    public void Jump() { animator.SetTrigger("Jump"); }
    public void Land() { animator.SetTrigger("Land"); }
}