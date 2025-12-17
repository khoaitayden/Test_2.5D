using UnityEngine;
using System.Collections.Generic;
// If this namespace errors, try "using Cinemachine;" instead
using Unity.Cinemachine;

public class WispMapLightController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    private Transform mainCameraTransform;

    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float pointLightFarClip = 40f;
    [SerializeField] private float visionLightFarClip = 100f;
    [SerializeField] private float focusLightFarClip = 60f;
    [SerializeField] private float offLightFarClip = 15f;

    [Header("Lights")]
    [SerializeField] private Light pointLight;
    [SerializeField] private Vector3 pointLightOffset = new Vector3(1.5f, 2.5f, -2.0f);

    [SerializeField] private Light visionLight;
    [SerializeField] private Vector3 visionLightOffset = new Vector3(0f, 1.8f, -1.0f);

    [SerializeField] private Light focusLight;
    [SerializeField] private Vector3 focusLightOffset = new Vector3(0f, 1.6f, -0.5f);

    [Header("Detection & Occlusion")]
    [Tooltip("Layers that trigger the 'Lit' effect (e.g., Monsters, Interactables)")]
    public LayerMask detectionLayer = -1;
    [Tooltip("Layers that block light (e.g., Default, Ground, Walls). DO NOT include the detection layer here.")]
    public LayerMask obstructionLayer = 1; 

    [Header("Floating & Movement")]
    [SerializeField] private float floatStrength = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float smoothTime = 0.3f;

    // Original settings
    private float origPointI, origPointR;
    private float origVisionI, origVisionR;
    private float origFocusI, origFocusR;

    private Vector3 pointVel = Vector3.zero;
    private Vector3 visionVel = Vector3.zero;
    private Vector3 focusVel = Vector3.zero;

    private enum LightMode { Point, Vision, Focus }
    private LightMode currentMode = LightMode.Point;
    private Light activeLight;
    
    // Set of objects currently considered "Lit"
    private HashSet<Collider> currentlyLit = new HashSet<Collider>();

    // New flag for Manual Toggle
    private bool isSystemPoweredOn = true;
    public bool IsLightActive { get; private set; } = true;
    void Start()
    {
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        CacheOriginalSettings();
        ApplyLightMode();

        // Subscribe to Input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnWispCycleTriggered += CycleLightMode;
            InputManager.Instance.OnWispPowerToggleTriggered += TogglePower;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnWispCycleTriggered -= CycleLightMode;
            InputManager.Instance.OnWispPowerToggleTriggered -= TogglePower;
        }
    }

    void Update()
    {
        if (isSystemPoweredOn)
        {
            ApplyGlobalLightEnergy();
        }

        UpdateLitObjects();
    }

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;

        Vector3 basePos = player.position;
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;

        UpdateLight(pointLight, pointLightOffset, ref pointVel, false, bob);
        UpdateLight(visionLight, visionLightOffset, ref visionVel, true, bob);
        UpdateLight(focusLight, focusLightOffset, ref focusVel, true, bob);
    }

    // --- Input Event Handlers ---

    void CycleLightMode()
    {
        if (!isSystemPoweredOn) return;

        currentMode = (LightMode)(((int)currentMode + 1) % 3);
        ApplyLightMode();
    }

    void TogglePower()
    {
        isSystemPoweredOn = !isSystemPoweredOn;

        if (isSystemPoweredOn)
        {
            // Turning ON
            if (LightEnergyManager.Instance != null)
                LightEnergyManager.Instance.SetDrainPaused(false);
            
            ApplyLightMode(); 
            // We only set IsLightActive if energy > 0, which ApplyLightMode handles via ApplyGlobalLightEnergy
        }
        else
        {
            // Turning OFF
            if (LightEnergyManager.Instance != null)
                LightEnergyManager.Instance.SetDrainPaused(true);
            
            TurnOffAllLights(); // This will set IsLightActive to false
        }
    }

    // --- Core Logic ---

    void ApplyGlobalLightEnergy()
    {
        if (LightEnergyManager.Instance == null) return;

        float energy = LightEnergyManager.Instance.GetIntensityFactor();
        
        if (energy <= 0f)
        {
            TurnOffAllLights(); // This will set IsLightActive to false
            return;
        }

        // --- NEW ---
        // If we have energy and system is on, light is active
        IsLightActive = isSystemPoweredOn; 
        // ---------

        float effectiveRange = energy;
        float effectiveIntensity = Mathf.Sqrt(Mathf.Clamp01(energy));

        ApplyDimmedSettings(effectiveRange, effectiveIntensity);
    }

    // --- Camera Helper ---

    void UpdateCameraClip(float farClipValue)
    {
        if (virtualCamera != null)
        {
            var lensSettings = virtualCamera.Lens;
            lensSettings.FarClipPlane = farClipValue;
            virtualCamera.Lens = lensSettings;
        }
    }

    // --- Light Management ---

    void ApplyLightMode()
    {
        if (!isSystemPoweredOn) return;

        TurnOffAllLights(); // Reset active light

        switch (currentMode)
        {
            case LightMode.Point:
                if (pointLight != null) { pointLight.enabled = true; activeLight = pointLight; }
                UpdateCameraClip(pointLightFarClip);
                break;
            case LightMode.Vision:
                if (visionLight != null) { visionLight.enabled = true; activeLight = visionLight; }
                UpdateCameraClip(visionLightFarClip);
                break;
            case LightMode.Focus:
                if (focusLight != null) { focusLight.enabled = true; activeLight = focusLight; }
                UpdateCameraClip(focusLightFarClip);
                break;
        }
    }

    void TurnOffAllLights()
    {
        if (pointLight != null) pointLight.enabled = false;
        if (visionLight != null) visionLight.enabled = false;
        if (focusLight != null) focusLight.enabled = false;
        activeLight = null;

        // --- NEW ---
        IsLightActive = false;
        // ---------

        UpdateCameraClip(offLightFarClip);
    }

    // --- Lit Object Logic (Detection & Occlusion) ---

    void UpdateLitObjects()
    {
        // 1. Get raw candidates via OverlapSphere/Spot
        Collider[] rawColliders = GetObjectsInLight();
        HashSet<Collider> validLitSet = new HashSet<Collider>();

        // 2. Filter candidates via Occlusion Raycast
        if (activeLight != null)
        {
            Vector3 lightPos = activeLight.transform.position;
            
            foreach (Collider col in rawColliders)
            {
                if (col == null) continue;

                // Check Line of Sight
                if (HasLineOfSight(lightPos, col))
                {
                    validLitSet.Add(col);
                }
            }
        }

        // 3. Process Logic (Lit vs Unlit)
        
        // Handle newly lit objects
        foreach (Collider col in validLitSet)
        {
            if (!currentlyLit.Contains(col))
            {
                NotifyLit(col, true);
            }
            else
            {
                // Still lit, drain energy
                DrainEnergyFrom(col);
            }
        }

        foreach (Collider col in currentlyLit)
        {
            if (!validLitSet.Contains(col))
            {
                NotifyLit(col, false);
            }
        }

        currentlyLit = validLitSet;
    }

    private bool HasLineOfSight(Vector3 origin, Collider target)
    {
        Vector3 targetCenter = target.bounds.center;
        Vector3 direction = targetCenter - origin;
        float distance = direction.magnitude;
        
        if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, distance, obstructionLayer))
        {
            return false; 
        }

        // No wall was hit.
        return true;
    }

    void NotifyLit(Collider col, bool isLit)
    {
        if (col == null) return;
        ILitObject litObj = col.GetComponent<ILitObject>();
        if (litObj != null)
        {
            if (isLit) litObj.OnLit();
            else litObj.OnUnlit();
        }
    }

    void DrainEnergyFrom(Collider col)
    {
        if (col == null) return;
        TombstoneController tomb = col.GetComponent<TombstoneController>();
        if (tomb != null) tomb.DrainEnergy(Time.deltaTime);
    }

    // --- Light Positioning Helpers ---

    void UpdateLight(Light light, Vector3 offset, ref Vector3 velocity, bool useCameraRotation, float bob)
    {
        if (light == null) return;

        Vector3 target = player.position +
            mainCameraTransform.right * offset.x +
            Vector3.up * (offset.y + bob) +
            mainCameraTransform.forward * offset.z;

        light.transform.position = Vector3.SmoothDamp(light.transform.position, target, ref velocity, smoothTime);

        if (useCameraRotation)
            light.transform.rotation = mainCameraTransform.rotation;
        else
            light.transform.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
    }

    void CacheOriginalSettings()
    {
        if (pointLight != null) { origPointI = pointLight.intensity; origPointR = pointLight.range; }
        if (visionLight != null) { origVisionI = visionLight.intensity; origVisionR = visionLight.range; }
        if (focusLight != null) { origFocusI = focusLight.intensity; origFocusR = focusLight.range; }
    }

    void ApplyDimmedSettings(float rangeFactor, float intensityFactor)
    {
        rangeFactor = Mathf.Clamp01(rangeFactor);
        intensityFactor = Mathf.Clamp01(intensityFactor);

        if (pointLight != null)
        {
            pointLight.range = origPointR * rangeFactor;
            pointLight.intensity = origPointI * intensityFactor;
        }
        if (visionLight != null)
        {
            visionLight.range = origVisionR * rangeFactor;
            visionLight.intensity = origVisionI * intensityFactor;
        }
        if (focusLight != null)
        {
            focusLight.range = origFocusR * rangeFactor;
            focusLight.intensity = origFocusI * intensityFactor;
        }
    }

    // --- Detection Areas ---

    public Collider[] GetObjectsInLight()
    {
        if (activeLight == null || !activeLight.enabled)
            return new Collider[0];

        float currentRange = activeLight.range;

        if (activeLight.type == LightType.Point)
        {
            return Physics.OverlapSphere(activeLight.transform.position, currentRange * 0.7f, detectionLayer);
        }
        else if (activeLight.type == LightType.Spot)
        {
            return OverlapSpot(
                activeLight.transform.position,
                activeLight.transform.forward,
                currentRange * 0.7f,
                activeLight.spotAngle,
                detectionLayer
            );
        }

        return new Collider[0];
    }

    private Collider[] OverlapSpot(Vector3 origin, Vector3 direction, float range, float angle, LayerMask layerMask)
    {
        List<Collider> results = new List<Collider>();
        if (range <= 0) return results.ToArray();

        Collider[] colliders = Physics.OverlapSphere(origin, range, layerMask);
        float halfAngleRad = angle * 0.5f * Mathf.Deg2Rad;

        foreach (Collider col in colliders)
        {
            Vector3 toCollider = col.transform.position - origin;
            if (toCollider.sqrMagnitude > range * range) continue;

            toCollider.Normalize();
            float dot = Vector3.Dot(direction, toCollider);

            if (dot >= Mathf.Cos(halfAngleRad))
            {
                results.Add(col);
            }
        }

        return results.ToArray();
    }
}