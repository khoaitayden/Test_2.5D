using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 20f; 
    public LayerMask groundLayer;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCameraTransform;

    public Vector3 WorldSpaceMoveDirection { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Ground Check (same as before)
        isGrounded = Physics.CheckSphere(transform.position + controller.center - new Vector3(0, controller.height / 2, 0), 0.2f, groundLayer, QueryTriggerInteraction.Ignore);
        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }

        // Movement Input (same as before)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * z + camRight * x).normalized;

        // --- ROTATION IS BACK ---
        // The parent object now smoothly rotates to face the movement direction.
        // This gives our animation script a stable reference.
        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Jumping and Gravity (same as before)
        if (Input.GetButtonDown("Jump") && isGrounded) { velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); }
        velocity.y += gravity * Time.deltaTime;

        // Apply final combined movement
        Vector3 finalMove = WorldSpaceMoveDirection * moveSpeed + velocity;
        controller.Move(finalMove * Time.deltaTime);
    }
}