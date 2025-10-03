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

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerParentTransform = playerController.transform; // Get the parent's transform
        animator = GetComponent<Animator>();
        mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
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

        Vector3 lookPos = mainCameraTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    public void Jump()
    {
        animator.SetTrigger("Jump");
    }

    public void Land()
    {
        animator.SetTrigger("Land");
    }
}