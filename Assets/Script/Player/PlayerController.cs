using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 20f; 
    [SerializeField] private LayerMask groundLayer;

    [Header("Reference")]
    [SerializeField] private PlayerParticleController particleController;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCameraTransform;
    public Vector3 WorldSpaceMoveDirection { get; private set; }
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        wasGrounded = true;
    }

    void Update()
    {

        isGrounded = Physics.CheckSphere(transform.position + controller.center - new Vector3(0, controller.height / 2, 0), 0.2f, groundLayer, QueryTriggerInteraction.Ignore);
        if (isGrounded != wasGrounded)
        {
            if (isGrounded)
            {
                float fallIntensity = Mathf.Abs(velocity.y);
                particleController.PlayLandEffect(fallIntensity);
            }
            wasGrounded = isGrounded;
            particleController.ToggleDirtTrail(isGrounded);
        }
        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();
        WorldSpaceMoveDirection = (camForward * z + camRight * x).normalized;

        if (WorldSpaceMoveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(WorldSpaceMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        if (Input.GetButtonDown("Jump") && isGrounded) { velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); }
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = WorldSpaceMoveDirection * moveSpeed + velocity;
        controller.Move(finalMove * Time.deltaTime);
    }
}