using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class WispMapLightController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    private Transform mainCameraTransform;

    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float pointLightFarClip = 40f;
    // Removed VisionLightFarClip
    [SerializeField] private float focusLightFarClip = 60f;
    [SerializeField] private float offLightFarClip = 15f;
    
    [Header("Energy Setting")]
    [SerializeField] private float energyThresholdBeforeCollect = 0.8f;
    
    [Header("Lights")]
    [SerializeField] private Light pointLight;
    [SerializeField] private Vector3 pointLightOffset = new Vector3(1.5f, 2.5f, -2.0f);

    // Removed Vision Light Variables

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

    // --- Private State Variables ---
    private float origPointI, origPointR;
    // Removed origVision vars
    private float origFocusI, origFocusR;
    
    private Vector3 pointVel = Vector3.zero;
    // Removed visionVel
    private Vector3 focusVel = Vector3.zero;
    
    // Updated Enum to only have Point and Focus
    private enum LightMode { Point, Focus }
    private LightMode currentMode = LightMode.Point;
    
    private Light activeLight;
    private TombstoneController _currentTargetTombstone;
    private bool isSystemPoweredOn = true;
    public bool IsLightActive { get; private set; } = true;

    void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        CacheOriginalSettings();
        ApplyLightMode();

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
        
        UpdateTombstoneTargeting();
    }

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;
        
        UpdateLight(pointLight, pointLightOffset, ref pointVel, false, bob);
        // Removed Vision Update
        UpdateLight(focusLight, focusLightOffset, ref focusVel, true, bob);
    }

    // --- MASTER LOGIC METHOD ---
    private void UpdateTombstoneTargeting()
    {
        HashSet<TombstoneController> potentialTargets = GetVisibleTombstones();
        TombstoneController bestTarget = FindClosestTombstoneToPlayer(potentialTargets);

        if (bestTarget != _currentTargetTombstone)
        {
            if (_currentTargetTombstone != null)
            {
                _currentTargetTombstone.OnUnlit();
            }

            _currentTargetTombstone = bestTarget;
            if (_currentTargetTombstone != null)
            {
                _currentTargetTombstone.OnLit();
            }
        }
        
        if (_currentTargetTombstone != null)
        {
            if (LightEnergyManager.Instance != null && LightEnergyManager.Instance.CurrentEnergy < energyThresholdBeforeCollect)
            {
                _currentTargetTombstone.DrainEnergy(Time.deltaTime);
            }
            else
            {
                _currentTargetTombstone.OnUnlit();
            }
        }
    }

    private HashSet<TombstoneController> GetVisibleTombstones()
    {
        HashSet<TombstoneController> visibleTombstones = new HashSet<TombstoneController>();
        if (activeLight == null) return visibleTombstones;

        Collider[] rawColliders = GetObjectsInLight();
        Vector3 lightPos = activeLight.transform.position;
        
        foreach (Collider col in rawColliders)
        {
            if (col == null) continue;
            
            if (HasLineOfSight(lightPos, col))
            {
                TombstoneController tombstone = col.GetComponent<TombstoneController>();
                if (tombstone != null)
                {
                    visibleTombstones.Add(tombstone);
                }
            }
        }
        return visibleTombstones;
    }

    private TombstoneController FindClosestTombstoneToPlayer(HashSet<TombstoneController> tombstones)
    {
        TombstoneController closest = null;
        float closestDistSqr = float.MaxValue;

        Vector3 playerPos = player.position;

        foreach (TombstoneController tombstone in tombstones)
        {
            float distSqr = (tombstone.transform.position - playerPos).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = tombstone;
            }
        }
        return closest;
    }

    private bool HasLineOfSight(Vector3 origin, Collider target)
    {
        Vector3 targetCenter = target.bounds.center;
        Vector3 direction = targetCenter - origin;
        float distance = direction.magnitude;
        return !Physics.Raycast(origin, direction, distance, obstructionLayer);
    }

    // --- System & Boilerplate Methods ---

    void CycleLightMode()
    {
        if (!isSystemPoweredOn) return;
        // Modulo 2 since we only have Point and Focus now
        currentMode = (LightMode)(((int)currentMode + 1) % 2);
        ApplyLightMode();
    }

    void TogglePower()
    {
        isSystemPoweredOn = !isSystemPoweredOn;
        if (isSystemPoweredOn)
        {
            if (LightEnergyManager.Instance != null) LightEnergyManager.Instance.SetDrainPaused(false);
            ApplyLightMode();
        }
        else
        {
            if (LightEnergyManager.Instance != null) LightEnergyManager.Instance.SetDrainPaused(true);
            TurnOffAllLights();
        }
    }

    void ApplyGlobalLightEnergy()
    {
        if (LightEnergyManager.Instance == null) return;
        float energy = LightEnergyManager.Instance.GetIntensityFactor();
        if (energy <= 0f) { TurnOffAllLights(); return; }
        IsLightActive = isSystemPoweredOn;
        float effectiveRange = energy;
        float effectiveIntensity = Mathf.Sqrt(Mathf.Clamp01(energy));
        ApplyDimmedSettings(effectiveRange, effectiveIntensity);
    }

    void UpdateCameraClip(float farClipValue)
    {
        if (virtualCamera != null)
        {
            var lensSettings = virtualCamera.Lens;
            lensSettings.FarClipPlane = farClipValue;
            virtualCamera.Lens = lensSettings;
        }
    }

    void ApplyLightMode()
    {
        if (!isSystemPoweredOn) return;
        TurnOffAllLights();
        switch (currentMode)
        {
            case LightMode.Point:
                if (pointLight != null) { pointLight.enabled = true; activeLight = pointLight; }
                UpdateCameraClip(pointLightFarClip);
                break;
            // Removed Vision Case
            case LightMode.Focus:
                if (focusLight != null) { focusLight.enabled = true; activeLight = focusLight; }
                UpdateCameraClip(focusLightFarClip);
                break;
        }
    }

    void TurnOffAllLights()
    {
        if (pointLight != null) pointLight.enabled = false;
        // Removed Vision disable
        if (focusLight != null) focusLight.enabled = false;
        activeLight = null;
        IsLightActive = false;
        UpdateCameraClip(offLightFarClip);
        
        if (_currentTargetTombstone != null)
        {
            _currentTargetTombstone.OnUnlit();
            _currentTargetTombstone = null;
        }
    }

    void UpdateLight(Light light, Vector3 offset, ref Vector3 velocity, bool useCameraRotation, float bob)
    {
        if (light == null) return;
        Vector3 target = player.position + mainCameraTransform.right * offset.x + Vector3.up * (offset.y + bob) + mainCameraTransform.forward * offset.z;
        light.transform.position = Vector3.SmoothDamp(light.transform.position, target, ref velocity, smoothTime);
        if (useCameraRotation)
            light.transform.rotation = mainCameraTransform.rotation;
        else
            light.transform.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
    }

    void CacheOriginalSettings()
    {
        if (pointLight != null) { origPointI = pointLight.intensity; origPointR = pointLight.range; }
        // Removed Vision Cache
        if (focusLight != null) { origFocusI = focusLight.intensity; origFocusR = focusLight.range; }
    }

    void ApplyDimmedSettings(float rangeFactor, float intensityFactor)
    {
        rangeFactor = Mathf.Clamp01(rangeFactor);
        intensityFactor = Mathf.Clamp01(intensityFactor);
        if (pointLight != null) { pointLight.range = origPointR * rangeFactor; pointLight.intensity = origPointI * intensityFactor; }
        // Removed Vision Dimming
        if (focusLight != null) { focusLight.range = origFocusR * rangeFactor; focusLight.intensity = origFocusI * intensityFactor; }
    }

    public Collider[] GetObjectsInLight()
    {
        if (activeLight == null || !activeLight.enabled) return new Collider[0];
        float currentRange = activeLight.range;
        
        if (activeLight.type == LightType.Point)
        {
            return Physics.OverlapSphere(activeLight.transform.position, currentRange * 0.7f, detectionLayer);
        }
        else if (activeLight.type == LightType.Spot)
        {
            return OverlapSpot(activeLight.transform.position, activeLight.transform.forward, currentRange * 0.7f, activeLight.spotAngle, detectionLayer);
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
            if (dot >= Mathf.Cos(halfAngleRad)) results.Add(col);
        }
        return results.ToArray();
    }
}