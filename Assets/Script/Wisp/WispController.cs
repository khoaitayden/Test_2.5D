using UnityEngine;

public class WispController : MonoBehaviour
{
    public static WispController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController; 
    [SerializeField] private Light innerGlowLight; // The small light on the wisp itself
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Orbit & Motion")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitHeight = 1.5f;
    [SerializeField] private float orbitSpeed = 40f;
    [SerializeField] private float followLag = 0.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer; 
    [SerializeField] private float collisionRadius = 0.5f;
    [SerializeField] private float avoidanceStrength = 5f;

    [Header("Camera Safety")]
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float minDistanceFromCamera = 1.0f;

    [Header("Soul Collection")]
    [SerializeField] private LayerMask absorbableLayer;
    [SerializeField] private float collectRadius = 4.0f;
    [SerializeField] private float absorbRate = 5.0f;
    [SerializeField] private float energyThreshold = 0.8f;

    [Header("Visual Config")]
    [SerializeField] private float maxGlowIntensity = 2f;
    [SerializeField] private float minGlowIntensity = 0.5f;

    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;
    private Collider[] hitColliders = new Collider[5]; 
    private TombstoneController activeTombstone;

    // Public getter: Is the Wisp alive?
    public bool IsWispAlive 
    {
        get { return LightEnergyManager.Instance != null && LightEnergyManager.Instance.CurrentEnergy > 0; }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        orbitAngle = Random.Range(0f, 360f);
    }

    void FixedUpdate()
    {
        if (playerTransform == null || mainCameraTransform == null) return;

        // 1. Movement Logic
        MoveWisp();

        // 2. Visuals (Only show if alive)
        UpdateVisuals();

        // 3. Logic (Only collect if alive)
        if (IsWispAlive) 
        {
            HandleTombstoneInteraction();
        }
        else
        {
            // Disconnect if dead
            if (activeTombstone != null)
            {
                activeTombstone.OnUnlit(LightSourceType.Wisp);
                activeTombstone = null;
            }
        }
    }

    void MoveWisp()
    {
        // Orbit
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );

        // Lag
        Vector3 lagOffset = Vector3.zero;
        if (playerController != null && playerController.WorldSpaceMoveDirection.magnitude > 0.1f)
        {
            lagOffset = -playerTransform.forward * followLag;
        }

        // Target Calculation
        Vector3 targetPos = playerTransform.position + orbitOffset + lagOffset + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        // Obstacle Avoidance
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, collisionRadius, hitColliders, obstacleLayer);
        Vector3 avoidance = Vector3.zero;
        if (numHits > 0)
        {
            for (int i = 0; i < numHits; i++)
            {
                if (hitColliders[i] == null) continue;
                Vector3 pushDir = transform.position - hitColliders[i].ClosestPoint(transform.position);
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.up;
                avoidance += pushDir.normalized * (1f - Mathf.Clamp01(pushDir.magnitude / collisionRadius)) * avoidanceStrength;
            }
        }

        // Camera Clip Prevention
        Vector3 finalPos = targetPos + avoidance;
        Vector3 toWisp = finalPos - mainCameraTransform.position;
        if (toWisp.magnitude < minDistanceFromCamera)
        {
            finalPos = mainCameraTransform.position + toWisp.normalized * minDistanceFromCamera;
        }

        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, 0.2f);
    }

    void UpdateVisuals()
    {
        // 1. Billboarding
        if (mainCameraTransform != null)
        {
            Vector3 flatForward = mainCameraTransform.forward;
            flatForward.y = 0;
            if (flatForward.magnitude > 0.1f) transform.rotation = Quaternion.LookRotation(flatForward);
        }

        // 2. Inner Glow Logic
        if (LightEnergyManager.Instance == null) return;

        float energyFactor = LightEnergyManager.Instance.GetIntensityFactor();
        bool isAlive = LightEnergyManager.Instance.CurrentEnergy > 0;

        // Toggle Sprite
        if (spriteRenderer != null) spriteRenderer.enabled = isAlive;

        // Toggle Inner Light
        if (innerGlowLight != null)
        {
            innerGlowLight.enabled = isAlive;
            if (isAlive)
            {
                // Pulsating effect for life
                float pulse = Mathf.Lerp(0.8f, 1.2f, Mathf.PerlinNoise(Time.time * 3f, 0f));
                innerGlowLight.intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, energyFactor) * pulse;
            }
        }
    }

    void HandleTombstoneInteraction()
    {
        if (LightEnergyManager.Instance == null) return;
        
        Collider[] hits = Physics.OverlapSphere(transform.position, collectRadius, absorbableLayer);
        TombstoneController nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < minDist)
            {
                var t = hit.GetComponent<TombstoneController>();
                if (t != null && t.CurrentEnergy > 0) { nearest = t; minDist = d; }
            }
        }

        if (nearest != activeTombstone)
        {
            if (activeTombstone != null) activeTombstone.OnUnlit(LightSourceType.Wisp);
            activeTombstone = nearest;
            if (activeTombstone != null) activeTombstone.OnLit(LightSourceType.Wisp);
        }

        if (activeTombstone != null && LightEnergyManager.Instance.EnergyFraction < energyThreshold)
        {
            float amount = absorbRate * Time.deltaTime;
            activeTombstone.DrainEnergyByAmount(amount);
            LightEnergyManager.Instance.RestoreEnergy(amount);
        }
    }
}