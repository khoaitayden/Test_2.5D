using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCameraTransform;

    public Vector3 WorldSpaceMoveDirection { get; private set; }

    // We need a stable forward direction for animation calculations
    public Vector3 ForwardDirection { get; private set; } = Vector3.forward;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        Vector3 spherePosition = transform.position + controller.center - new Vector3(0, controller.height / 2, 0);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
        Debug.Log("Is Grounded: " + isGrounded);

        if (isGrounded && velocity.y < 0) velocity.y = -1f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        WorldSpaceMoveDirection = (camForward * z + camRight * x).normalized;

        controller.Move(WorldSpaceMoveDirection * moveSpeed * Time.deltaTime);

        // --- IMPORTANT CHANGE ---
        // If we are moving, update our logical "ForwardDirection"
        if (WorldSpaceMoveDirection.sqrMagnitude > 0.01f)
        {
            ForwardDirection = WorldSpaceMoveDirection;
        }

        // Jumping and Gravity
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}