using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private Transform mainCameraTransform;
    private Transform playerParentTransform;
    private PlayerController playerController;

    private readonly int animHorizontal = Animator.StringToHash("HorizontalInput");
    private readonly int animVertical = Animator.StringToHash("VerticalInput");
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animIsClimbing = Animator.StringToHash("IsClimbing"); // Add this Parameter to Animator!

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerParentTransform = playerController.transform;
        animator = GetComponent<Animator>();
        mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // 1. BILLBOARDING (Always look at camera)
        Vector3 lookPos = mainCameraTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        // 2. HANDLE CLIMBING
        if (playerController.IsClimbing)
        {
            //animator.SetBool(animIsClimbing, true);
            
            // FORCE VISUALS:
            // When climbing, we usually want to show the "Back" sprite (walking away).
            // In a blend tree, this is usually Vertical = 1.0, Horizontal = 0.0
            animator.SetFloat(animVertical, 1.0f);
            animator.SetFloat(animHorizontal, 0.0f);
            animator.SetFloat(animSpeed, InputManager.Instance.MoveInput.magnitude);
            return; // Skip normal calculation
        }
        
        //animator.SetBool(animIsClimbing, false);

        // 3. NORMAL CALCULATION
        Vector3 playerForward = playerParentTransform.forward;
        Vector3 cameraDirection = playerParentTransform.position - mainCameraTransform.position;
        cameraDirection.y = 0;
        cameraDirection.Normalize();

        float verticalInput = Vector3.Dot(cameraDirection, playerForward);
        Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
        float horizontalInput = Vector3.Dot(cameraDirection, playerRight);

        animator.SetFloat(animVertical, verticalInput);
        animator.SetFloat(animHorizontal, horizontalInput);
        animator.SetFloat(animSpeed, playerController.WorldSpaceMoveDirection.magnitude);
    }

    public void Jump() { animator.SetTrigger("Jump"); }
    public void Land() { animator.SetTrigger("Land"); }
}