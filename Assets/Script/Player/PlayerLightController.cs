using UnityEngine;

public class PlayerLightController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    private Transform mainCameraTransform;

    [Header("Positioning")]
    [Tooltip("Controls the light's position. X is left/right of player, Y is above player, Z is distance from player towards the camera.")]
    public Vector3 followOffset = new Vector3(1.5f, 2.5f, -2.0f);

    [Header("Floating Motion")]
    public float floatStrength = 0.2f;
    public float floatSpeed = 2.0f;

    [Header("Smooth Follow")]
    public float smoothTime = 0.3f;
    
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;

        // --- 1. Calculate the Base Target Position ---

        // Start at the player's position.
        Vector3 targetPosition = player.position;

        // Use the CAMERA'S orientation to apply the offset. This is the key.
        // It ensures the light is always positioned relative to the viewing angle.
        targetPosition += mainCameraTransform.right * followOffset.x; // Move left/right of player on screen
        targetPosition += Vector3.up * followOffset.y;                 // Move above player
        
        // Move towards the camera from the player. A negative Z keeps it in front of the sprite.
        targetPosition += mainCameraTransform.forward * followOffset.z;


        // --- 2. Add Floating Motion ---
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;
        targetPosition += Vector3.up * bob;
        

        // --- 3. Smoothly Move the Light ---
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
}