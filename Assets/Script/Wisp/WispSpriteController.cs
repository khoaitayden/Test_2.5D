using UnityEngine;

public class WispController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform;
    public PlayerController playerController;

    [Header("Positioning")]
    public Vector3 behindOffset = new Vector3(0f, 1.5f, -2.5f);
    public Vector3 frontOffset = new Vector3(1.2f, 1.0f, 1.8f);
    public float minDistanceFromCamera = 2f; 

    [Header("Smooth Following")]
    public float followSmoothTime = 0.5f;

    [Header("Lively Motion")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;
    public float swaySpeed = 0.8f;
    public float swayRadius = 0.4f;

    // Internal variables
    public Transform mainCameraTransform;
    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {

        // Safety check to prevent errors if references are not set
        if (playerTransform == null || playerController == null)
        {
            Debug.LogError("WispController is missing references to the Player!", this);
            enabled = false; // Disable the script
        }
    }

    // Use LateUpdate to ensure the wisp moves after the player and camera have finished their updates
    void LateUpdate()
    {
        if (!enabled) return;

        // --- 1. Determine the Target Position based on Player State ---

        // Check if the player is moving by looking at the magnitude of their move direction vector
        bool isPlayerMoving = playerController.WorldSpaceMoveDirection.magnitude > 0.1f;

        // Choose the correct offset based on whether the player is moving or idle
        Vector3 targetOffset = isPlayerMoving ? behindOffset : frontOffset;

        // Calculate the base target position in world space, relative to the player's rotation
        Vector3 baseTargetPosition = playerTransform.position
                                   + (playerTransform.right * targetOffset.x)
                                   + (playerTransform.up * targetOffset.y)
                                   + (playerTransform.forward * targetOffset.z);

        // --- 2. Add Lively Motion ---

        // Vertical Bobbing (using a smooth sine wave)
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        // Horizontal Sway (using sine and cosine for a circular pattern)
        float swayAngle = Time.time * swaySpeed;
        Vector3 swayOffset = new Vector3(Mathf.Cos(swayAngle), 0, Mathf.Sin(swayAngle)) * swayRadius;

        // Combine all parts to get the final target position
        Vector3 finalTargetPosition = baseTargetPosition + new Vector3(0, bobOffset, 0) + swayOffset;

        // --- 3. Smoothly Move the Wisp ---

        // Use SmoothDamp to create a fluid, damped movement towards the target
        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalTargetPosition,
            ref currentVelocity,
            followSmoothTime
        );

        // --- 4. Make the Sprite Face the Camera (Billboarding) ---
        Vector3 cameraForward = mainCameraTransform.forward;
        cameraForward.y = 0; // Ignore tilt up/down
        transform.rotation = Quaternion.LookRotation(cameraForward);

        
        Vector3 cameraToWisp = transform.position - mainCameraTransform.position;
        if (cameraToWisp.magnitude < minDistanceFromCamera)
        {
            transform.position = mainCameraTransform.position + cameraToWisp.normalized * minDistanceFromCamera;
        }
    }
}