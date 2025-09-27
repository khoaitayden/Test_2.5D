using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10f; // Speed at which the character turns

    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    
    // --- NEW ---
    private Transform mainCameraTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
                Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(
            transform.position - new Vector3(0, controller.height / 2, 0),
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded && velocity.y < 0)
            velocity.y = 0f;

        // Movement input (remains the same)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // --- MODIFIED: Calculate movement direction relative to the camera ---
        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;

        // We want to ignore the camera's vertical angle
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // This is the direction we want to move in, based on camera and input
        Vector3 moveDirection = (camForward * z + camRight * x).normalized;

        // Apply movement
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // --- NEW: Rotate the player to face the movement direction ---
        if (moveDirection != Vector3.zero)
        {
            // Create a rotation that looks in the calculated direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Smoothly interpolate from the current rotation to the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Jumping (remains the same)
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Apply gravity (remains the same)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}