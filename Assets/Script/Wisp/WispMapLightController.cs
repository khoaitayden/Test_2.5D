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
    [SerializeField] private float wispLightFarClip = 40f; 
    [SerializeField] private float flashLightFarClip = 60f; 
    [SerializeField] private float offLightFarClip = 15f;
    
    [Header("Energy Setting")]
    [SerializeField] private float energyThresholdBeforeCollect = 0.8f;
    
    [Header("Wisp Light (Always On)")]
    [SerializeField] private Light pointLight;
    [SerializeField] private Vector3 pointLightOffset = new Vector3(1.5f, 2.5f, -2.0f);

    [Header("Flashlight (Temporary)")]
    [SerializeField] private Light focusLight;
    [SerializeField] private Vector3 focusLightOffset = new Vector3(0f, 1.6f, -0.5f);
    [SerializeField] private float flashLightDuration = 5.0f; 
    [SerializeField] private float flashLightCooldown = 3.0f; 

    [Header("Detection & Occlusion")]
    public LayerMask detectionLayer = -1;
    public LayerMask obstructionLayer = 1;

    [Header("Floating & Movement")]
    [SerializeField] private float floatStrength = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float smoothTime = 0.3f;

    private float origPointI, origPointR;
    private float origFocusI, origFocusR;
    private Vector3 pointVel = Vector3.zero;
    
    private TombstoneController _currentTargetTombstone;
    
    private bool isSystemPoweredOn = true;
    public bool IsLightActive { get; private set; } = true;

    private bool isFlashlightActive = false;
    private float flashLightTimer = 0f;
    private float flashLightCooldownTimer = 0f;

    void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        CacheOriginalSettings();
        UpdateLightState();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnWispCycleTriggered += TryToggleFlashlight;
            InputManager.Instance.OnWispPowerToggleTriggered += ToggleSystemPower;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnWispCycleTriggered -= TryToggleFlashlight;
            InputManager.Instance.OnWispPowerToggleTriggered -= ToggleSystemPower;
        }
    }

    void Update()
    {
        HandleFlashlightTimers();

        if (isSystemPoweredOn)
        {
            ApplyGlobalLightEnergy();
        }
        else
        {
            IsLightActive = false;
        }
        
        UpdateTombstoneTargeting();
    }

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;
        
        UpdateLightPosition(pointLight, pointLightOffset, ref pointVel, false, bob);
        
        if (focusLight != null)
        {
            Vector3 target = player.position +
                mainCameraTransform.right * focusLightOffset.x +
                Vector3.up * (focusLightOffset.y + bob) +
                mainCameraTransform.forward * focusLightOffset.z;
            
            focusLight.transform.position = target;
            focusLight.transform.rotation = mainCameraTransform.rotation;
        }
    }

    // --- FLASHLIGHT & POWER ---

    void TryToggleFlashlight()
    {
        if (!isSystemPoweredOn || Time.time < flashLightCooldownTimer) return;
        if (isFlashlightActive) TurnOffFlashlight();
        else TurnOnFlashlight();
    }

    void TurnOnFlashlight()
    {
        isFlashlightActive = true;
        flashLightTimer = Time.time + flashLightDuration;
        UpdateLightState();
    }

    void TurnOffFlashlight()
    {
        if (!isFlashlightActive) return;
        isFlashlightActive = false;
        flashLightCooldownTimer = Time.time + flashLightCooldown;
        UpdateLightState();
    }

    void HandleFlashlightTimers()
    {
        if (isFlashlightActive && Time.time > flashLightTimer) TurnOffFlashlight();
    }

    void ToggleSystemPower()
    {
        isSystemPoweredOn = !isSystemPoweredOn;
        if (isSystemPoweredOn)
        {
            if (LightEnergyManager.Instance != null) LightEnergyManager.Instance.SetDrainPaused(false);
        }
        else
        {
            if (LightEnergyManager.Instance != null) LightEnergyManager.Instance.SetDrainPaused(true);
            isFlashlightActive = false; 
        }
        UpdateLightState();
    }

    void UpdateLightState()
    {
        if (!isSystemPoweredOn)
        {
            if (pointLight != null) pointLight.enabled = false;
            if (focusLight != null) focusLight.enabled = false;
            IsLightActive = false;
            UpdateCameraClip(offLightFarClip);
            
            // FIX 1: Pass LightSourceType.Wisp
            if (_currentTargetTombstone != null)
            {
                _currentTargetTombstone.OnUnlit(LightSourceType.Wisp);
                _currentTargetTombstone = null;
            }
            return;
        }

        IsLightActive = true;
        if (pointLight != null) pointLight.enabled = true;
        if (focusLight != null) focusLight.enabled = isFlashlightActive;
        UpdateCameraClip(isFlashlightActive ? flashLightFarClip : wispLightFarClip);
    }

    void ApplyGlobalLightEnergy()
    {
        if (LightEnergyManager.Instance == null) return;
        float energy = LightEnergyManager.Instance.GetIntensityFactor();
        if (energy <= 0f) { if (isSystemPoweredOn) ToggleSystemPower(); return; }

        float effectiveRange = energy;
        float effectiveIntensity = Mathf.Sqrt(Mathf.Clamp01(energy));
        ApplyDimmedSettings(effectiveRange, effectiveIntensity);
    }

    // --- TARGETING ---

    private void UpdateTombstoneTargeting()
    {
        HashSet<TombstoneController> potentialTargets = GetVisibleTombstones();
        TombstoneController bestTarget = FindClosestTombstoneToPlayer(potentialTargets);

        if (bestTarget != _currentTargetTombstone)
        {
            // FIX 2: Pass LightSourceType.Wisp
            if (_currentTargetTombstone != null) 
                _currentTargetTombstone.OnUnlit(LightSourceType.Wisp);
            
            _currentTargetTombstone = bestTarget;
            
            // FIX 3: Pass LightSourceType.Wisp
            if (_currentTargetTombstone != null) 
                _currentTargetTombstone.OnLit(LightSourceType.Wisp);
        }
        
        if (_currentTargetTombstone != null)
        {
            if (LightEnergyManager.Instance != null && LightEnergyManager.Instance.CurrentEnergy < energyThresholdBeforeCollect)
            {
                _currentTargetTombstone.DrainEnergy(Time.deltaTime);
            }
            else
            {
                // FIX 4: Pass LightSourceType.Wisp
                _currentTargetTombstone.OnUnlit(LightSourceType.Wisp);
            }
        }
    }

    private HashSet<TombstoneController> GetVisibleTombstones()
    {
        HashSet<TombstoneController> visibleTombstones = new HashSet<TombstoneController>();
        if (!isSystemPoweredOn) return visibleTombstones;

        if (pointLight != null && pointLight.enabled) CheckLightForTombstones(pointLight, visibleTombstones);
        if (focusLight != null && focusLight.enabled) CheckLightForTombstones(focusLight, visibleTombstones);

        return visibleTombstones;
    }

    private void CheckLightForTombstones(Light light, HashSet<TombstoneController> visibleSet)
    {
        Collider[] rawColliders = GetCollidersInLightRange(light);
        Vector3 lightPos = light.transform.position;

        foreach (Collider col in rawColliders)
        {
            if (col == null) continue;
            TombstoneController tombstone = col.GetComponent<TombstoneController>();
            if (tombstone == null || visibleSet.Contains(tombstone)) continue;

            if (HasLineOfSight(lightPos, col))
            {
                visibleSet.Add(tombstone);
            }
        }
    }

    public Collider[] GetCollidersInLightRange(Light light)
    {
        float currentRange = light.range;
        if (light.type == LightType.Point)
        {
            return Physics.OverlapSphere(light.transform.position, currentRange * 0.7f, detectionLayer);
        }
        else if (light.type == LightType.Spot)
        {
            return OverlapSpot(light.transform.position, light.transform.forward, currentRange * 0.7f, light.spotAngle, detectionLayer);
        }
        return new Collider[0];
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

    void UpdateLightPosition(Light light, Vector3 offset, ref Vector3 velocity, bool useCameraRotation, float bob)
    {
        if (light == null) return;
        Vector3 target = player.position + mainCameraTransform.right * offset.x + Vector3.up * (offset.y + bob) + mainCameraTransform.forward * offset.z;
        light.transform.position = Vector3.SmoothDamp(light.transform.position, target, ref velocity, smoothTime);
        if (useCameraRotation)
            light.transform.rotation = mainCameraTransform.rotation;
        else
            light.transform.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
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

    void CacheOriginalSettings()
    {
        if (pointLight != null) { origPointI = pointLight.intensity; origPointR = pointLight.range; }
        if (focusLight != null) { origFocusI = focusLight.intensity; origFocusR = focusLight.range; }
    }

    void ApplyDimmedSettings(float rangeFactor, float intensityFactor)
    {
        rangeFactor = Mathf.Clamp01(rangeFactor);
        intensityFactor = Mathf.Clamp01(intensityFactor);
        if (pointLight != null) { pointLight.range = origPointR * rangeFactor; pointLight.intensity = origPointI * intensityFactor; }
        if (focusLight != null) { focusLight.range = origFocusR * rangeFactor; focusLight.intensity = origFocusI * intensityFactor; }
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