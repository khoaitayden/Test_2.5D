using UnityEngine;

public class WispController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform;
    public PlayerController playerController;

    [Header("Orbit Settings")]
    public float orbitRadius = 2f;           // Distance from player
    public float orbitHeight = 1.5f;         // Base height above player
    public float orbitSpeed = 40f;           // Degrees per second orbit
    public float followLag = 0.5f;           // How much the orbit lags behind when moving

    [Header("Lively Motion")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;

    [Header("Camera Safety")]
    public Transform mainCameraTransform;
    public float minDistanceFromCamera = 2f;

    [Header("Inner Glow")]
    public Light innerGlowLight;
    public float maxGlowIntensity;
    public float minGlowIntensity;
    public float innerGlowSpeed;

    // Internal variables
    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;

    void Start()
    {
        if (playerTransform == null || playerController == null)
        {
            Debug.LogError("WispController is missing references to the Player!", this);
            enabled = false;
        }

        orbitAngle = Random.Range(0f, 360f); // start at random orbit position
    }

    void LateUpdate()
    {
        if (!enabled) return;

        // --- 1. Orbit angle progression ---
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        // --- 2. Base orbit around player ---
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );

        // --- 3. Add trailing effect when moving ---
        Vector3 lagOffset = Vector3.zero;
        if (playerController.WorldSpaceMoveDirection.magnitude > 0.1f)
        {
            lagOffset = -playerTransform.forward * followLag;
        }

        // --- 4. Bobbing motion ---
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        // --- 5. Combine ---
        Vector3 finalTargetPosition = playerTransform.position
                                    + orbitOffset
                                    + lagOffset
                                    + new Vector3(0, bobOffset, 0);

        // Smooth movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalTargetPosition,
            ref currentVelocity,
            0.2f
        );

        // --- 6. Face camera (billboarding) ---
        if (mainCameraTransform != null)
        {
            Vector3 cameraForward = mainCameraTransform.forward;
            cameraForward.y = 0;
            transform.rotation = Quaternion.LookRotation(cameraForward);
        }

        // --- 7. Camera safety ---
        Vector3 cameraToWisp = transform.position - mainCameraTransform.position;
        if (cameraToWisp.magnitude < minDistanceFromCamera)
        {
            transform.position = mainCameraTransform.position + cameraToWisp.normalized * minDistanceFromCamera;
        }

        // --- 8. Inner glow pulsation ---
        float noise1 = Mathf.PerlinNoise(Time.time * 0.7f, 0f);
        float noise2 = Mathf.PerlinNoise(Time.time * 3.1f, 100f);
        float noise3 = Mathf.PerlinNoise(Time.time * 10f, 200f) * 0.3f;

        float combinedNoise = noise1 * 0.5f + noise2 * 0.4f + noise3 * 0.1f;
        combinedNoise = Mathf.Clamp01(combinedNoise); 

        float glowIntensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, combinedNoise);
        innerGlowLight.intensity = glowIntensity;
    }
}
