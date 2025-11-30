using UnityEngine;
using System.Collections.Generic;

public class WispMapLightController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    private Transform mainCameraTransform;

    [Header("Lights")]
    [SerializeField] private Light pointLight;
    [SerializeField] private Vector3 pointLightOffset = new Vector3(1.5f, 2.5f, -2.0f);

    [SerializeField] private Light visionLight;
    [SerializeField] private Vector3 visionLightOffset = new Vector3(0f, 1.8f, -1.0f);

    [SerializeField] private Light focusLight;
    [SerializeField] private Vector3 focusLightOffset = new Vector3(0f, 1.6f, -0.5f);

    [Header("Detection")]
    public LayerMask detectionLayer = -1;

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
    private HashSet<Collider> currentlyLit = new HashSet<Collider>();

    // New flag for Manual Toggle
    private bool isSystemPoweredOn = true;

    void Start()
    {
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        CacheOriginalSettings();
        ApplyLightMode();

        // Subscribe to Input
        if (InputManager.Instance != null)
        {
            // Short Press = Cycle Mode
            InputManager.Instance.OnWispCycleTriggered += CycleLightMode;
            // Long Hold = Toggle Power
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
        // Only calculate dimming/energy if the system is actually On
        if (isSystemPoweredOn)
        {
            ApplyGlobalLightEnergy();
        }
        
        UpdateLitObjects();
    }

    // --- Input Event Handlers ---

    void CycleLightMode()
    {
        // Don't change modes if the light is turned off
        if (!isSystemPoweredOn) return;

        currentMode = (LightMode)(((int)currentMode + 1) % 3);
        ApplyLightMode();
    }

    void TogglePower()
    {
        isSystemPoweredOn = !isSystemPoweredOn;

        if (isSystemPoweredOn)
        {
            // Turning ON: Resume Drain, Restore Light
            if (LightEnergyManager.Instance != null)
                LightEnergyManager.Instance.SetDrainPaused(false);
            
            ApplyLightMode(); 
        }
        else
        {
            // Turning OFF: Pause Drain, Kill Light
            if (LightEnergyManager.Instance != null)
                LightEnergyManager.Instance.SetDrainPaused(true);
            
            TurnOffAllLights();
        }
        
    }

    // --- Core Logic ---

    void ApplyGlobalLightEnergy()
    {
        if (LightEnergyManager.Instance == null) return;

        float energy = LightEnergyManager.Instance.GetIntensityFactor();
        
        // Even if system is ON, if energy is 0, we must turn off
        if (energy <= 0f)
        {
            TurnOffAllLights();
            return;
        }

        float effectiveRange = energy;
        float effectiveIntensity = Mathf.Sqrt(Mathf.Clamp01(energy));

        ApplyDimmedSettings(effectiveRange, effectiveIntensity);
    }

    // ... (LateUpdate, UpdateLitObjects, NotifyLit, DrainEnergyFrom, UpdateLight, CacheOriginalSettings, ApplyDimmedSettings, GetObjectsInLight REMAIN EXACTLY THE SAME AS BEFORE) ...

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;

        Vector3 basePos = player.position;
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;

        // Visuals update even if light is off (the physical object still floats)
        UpdateLight(pointLight, pointLightOffset, ref pointVel, false, bob);
        UpdateLight(visionLight, visionLightOffset, ref visionVel, true, bob);
        UpdateLight(focusLight, focusLightOffset, ref focusVel, true, bob);
    }
    
    // ... (Keep the rest of the existing methods below) ...
    
    void UpdateLitObjects()
    {
        Collider[] newlyLitColliders = GetObjectsInLight();
        HashSet<Collider> newLitSet = new HashSet<Collider>(newlyLitColliders);

        foreach (Collider col in newLitSet)
        {
            if (!currentlyLit.Contains(col)) NotifyLit(col, true);
            else DrainEnergyFrom(col);
        }

        foreach (Collider col in currentlyLit)
        {
            if (!newLitSet.Contains(col)) NotifyLit(col, false);
        }

        currentlyLit = newLitSet;
    }

    void NotifyLit(Collider col, bool isLit)
    {
        ILitObject litObj = col.GetComponent<ILitObject>();
        if (litObj != null)
        {
            if (isLit) litObj.OnLit();
            else litObj.OnUnlit();
        }
    }

    void DrainEnergyFrom(Collider col)
    {
        TombstoneController tomb = col.GetComponent<TombstoneController>();
        if (tomb != null) tomb.DrainEnergy(Time.deltaTime);
    }

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

    void ApplyLightMode()
    {
        // If system is manually off, don't turn on lights
        if (!isSystemPoweredOn) return; 

        TurnOffAllLights();
        switch (currentMode)
        {
            case LightMode.Point:
                if (pointLight != null) { pointLight.enabled = true; activeLight = pointLight; }
                break;
            case LightMode.Vision:
                if (visionLight != null) { visionLight.enabled = true; activeLight = visionLight; }
                break;
            case LightMode.Focus:
                if (focusLight != null) { focusLight.enabled = true; activeLight = focusLight; }
                break;
        }
    }

    void TurnOffAllLights()
    {
        if (pointLight != null) pointLight.enabled = false;
        if (visionLight != null) visionLight.enabled = false;
        if (focusLight != null) focusLight.enabled = false;
        activeLight = null;
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