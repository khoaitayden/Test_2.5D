using UnityEngine;

public class WispController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitHeight = 1.5f;
    [SerializeField] private float orbitSpeed = 40f;
    [SerializeField] private float followLag = 0.5f;

    [Header("Lively Motion")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Layers the Wisp should dodge (e.g., Default, Ground). Don't include Player!")]
    [SerializeField] private LayerMask obstacleLayer; 
    [Tooltip("How large is the bubble around the wisp for detection?")]
    [SerializeField] private float collisionRadius = 0.5f;
    [Tooltip("How hard the wisp pushes away from walls.")]
    [SerializeField] private float avoidanceStrength = 5f;

    [Header("Camera Safety")]
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float minDistanceFromCamera = 2f;

    [Header("Inner Glow")]
    [SerializeField] private Light innerGlowLight;
    [SerializeField] private float maxGlowIntensity = 2f;
    [SerializeField] private float minGlowIntensity = 0.5f;

    [Header("Audio")]
    [SerializeField] private SoundDefinition wispLoopDef;
    private AudioSource _audioSource;
    private float _baseVolume;

    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;
    
    // Optimization for Physics allocation
    private Collider[] hitColliders = new Collider[5]; 

    void Start()
    {
        if (playerTransform == null || playerController == null)
        {
            Debug.LogError("WispController is missing references to the Player!", this);
            enabled = false;
            return;
        }

        orbitAngle = Random.Range(0f, 360f);

        // Initialize glow intensity based on starting energy
        if (innerGlowLight != null && LightEnergyManager.Instance != null)
        {
            float factor = LightEnergyManager.Instance.GetIntensityFactor();
            float baseGlow = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, factor);
            innerGlowLight.intensity = baseGlow;
        }

        if (SoundManager.Instance != null && wispLoopDef != null)
        {
            // Create the loop
            _audioSource = SoundManager.Instance.CreateLoop(wispLoopDef, this.transform);
            // Remember the volume set in the ScriptableObject
            _baseVolume = wispLoopDef.volume; 
        }
    }

    void LateUpdate()
    {
        if (!enabled || playerTransform == null || mainCameraTransform == null) return;

        // --- 1. Base Orbit Calculation ---
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );

        // --- 2. Follow Lag ---
        Vector3 lagOffset = Vector3.zero;
        if (playerController.WorldSpaceMoveDirection.magnitude > 0.1f)
        {
            lagOffset = -playerTransform.forward * followLag;
        }

        // --- 3. Bobbing ---
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        // --- 4. Calculate Initial Target ---
        Vector3 targetPos = playerTransform.position + orbitOffset + lagOffset + Vector3.up * bobOffset;

        // --- 5. OBSTACLE AVOIDANCE (New) ---
        // Check if the *current* wisp position is touching something, or if the *target* is inside something
        // We use the current position for the origin to push away from what we are currently touching
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, collisionRadius, hitColliders, obstacleLayer);

        Vector3 avoidanceVector = Vector3.zero;

        if (numHits > 0)
        {
            for (int i = 0; i < numHits; i++)
            {
                Collider hit = hitColliders[i];
                if (hit == null) continue;

                // Find the closest point on the obstacle to the Wisp
                Vector3 closestPoint = hit.ClosestPoint(transform.position);
                
                // Calculate direction AWAY from the obstacle
                Vector3 pushDir = transform.position - closestPoint;
                
                // Prevent divide by zero if exactly inside
                if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector3.up;

                // The closer we are, the stronger the push
                float distance = pushDir.magnitude;
                float pushFactor = 1f - Mathf.Clamp01(distance / collisionRadius);

                avoidanceVector += pushDir.normalized * pushFactor * avoidanceStrength;
            }
        }

        // Apply avoidance to the target
        targetPos += avoidanceVector;


        // --- 6. Apply Movement ---
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 0.2f);

        // --- 7. Billboard to camera ---
        Vector3 flatForward = mainCameraTransform.forward;
        flatForward.y = 0;
        if (flatForward.magnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(flatForward);

        // --- 8. Camera safety (Clip Prevention) ---
        Vector3 toWisp = transform.position - mainCameraTransform.position;
        if (toWisp.magnitude < minDistanceFromCamera)
        {
            transform.position = mainCameraTransform.position + toWisp.normalized * minDistanceFromCamera;
        }

        // --- 9. Inner Glow Logic ---
        UpdateEffects();
    }
    void OnDisable()
    {
        if (_audioSource != null) _audioSource.Stop();
    }

    private void UpdateEffects()
    {
        if (LightEnergyManager.Instance == null) return;

        // Get the global energy factor (0.0 to 1.0)
        float energyFactor = LightEnergyManager.Instance.GetIntensityFactor();

        // 1. Handle Light (Existing logic)
        if (innerGlowLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * 2f, 0f);
            float pulse = Mathf.Lerp(0.8f, 1.2f, noise);
            // Fade intensity based on energy
            innerGlowLight.intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, energyFactor) * pulse;
        }

        // 2. Handle Audio Volume (New logic)
        if (_audioSource != null)
        {
            // If energy is 1, volume is max. If energy is 0, volume is 0.
            _audioSource.volume = _baseVolume * energyFactor;
            
            // Optional: Pitch shift slightly as it dies (sounds cool)
            _audioSource.pitch = Mathf.Lerp(0.5f, 1.0f, energyFactor);
        }
    }
}