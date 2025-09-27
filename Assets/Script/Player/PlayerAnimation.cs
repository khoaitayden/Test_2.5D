using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private Transform mainCameraTransform;
    private PlayerController playerController;

    private readonly int animHorizontal = Animator.StringToHash("HorizontalInput");
    private readonly int animVertical = Animator.StringToHash("VerticalInput");
    private readonly int animSpeed = Animator.StringToHash("Speed");

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        mainCameraTransform = Camera.main.transform;
    }
    
    void LateUpdate()
    {
        // --- Part 1: Calculate Directional Animation ---

        Vector3 playerForward = playerController.ForwardDirection;

        // --- THIS IS THE FIX ---
        // Get the direction FROM the camera TOWARDS the player
        Vector3 cameraDirection = transform.position - mainCameraTransform.position;
        cameraDirection.y = 0;
        cameraDirection.Normalize();

        float verticalInput = Vector3.Dot(cameraDirection, playerForward);
        Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
        float horizontalInput = Vector3.Dot(cameraDirection, playerRight);
        
        animator.SetFloat(animVertical, verticalInput);
        animator.SetFloat(animHorizontal, horizontalInput);

        float currentSpeed = playerController.WorldSpaceMoveDirection.magnitude;
        animator.SetFloat(animSpeed, currentSpeed);

        // --- Part 2: Rotate the Sprite to Face the Camera (Billboarding) ---
        Vector3 lookPos = mainCameraTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }
}