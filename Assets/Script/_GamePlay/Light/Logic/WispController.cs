using UnityEngine;
using System.Collections.Generic;

public class WispController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy; // Drag "var_CurrentEnergy"
    [SerializeField] private FloatVariableSO maxEnergy;  
    [Header("Lights")]
    [SerializeField] private Light innerGlowLight;
    [SerializeField] private Light areaMapLight; 
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Soul Collection")]
    [SerializeField] private LayerMask detectionLayer;   
    [SerializeField] private LayerMask obstructionLayer; 
    [Tooltip("Stops collecting if Energy % is higher than this (0.0 to 1.0)")]
    [SerializeField] private float energyThreshold = 0.8f;

    // --- State ---
    private TombstoneController _currentTargetTombstone;
    private HashSet<ILitObject> _currentlyLitObjects = new HashSet<ILitObject>();
    
    private float _initInnerIntensity;
    private float _initAreaIntensity;
    private float _initAreaRange;
    private float energyFactor;


    void Start()
    {
        if (innerGlowLight) _initInnerIntensity = innerGlowLight.intensity;
        if (areaMapLight)
        {
            _initAreaIntensity = areaMapLight.intensity;
            _initAreaRange = areaMapLight.range;
        }
    }

    void Update()
    {
        if (currentEnergy.Value<=0)
        {
            HandleDeath();
            return;
        }
        energyFactor = currentEnergy.Value / maxEnergy.Value;
        UpdateLights();
        UpdateInteractions();
    }

    // --- 1. LIGHT VISUALS (UPDATED) ---
    void UpdateLights()
    {
        // Sprite
        if (spriteRenderer) spriteRenderer.enabled = true;

        // Inner Light (Pulse)
        if (innerGlowLight)
        {
            innerGlowLight.enabled = true;
            float pulse = Mathf.Lerp(0.8f, 1.2f, Mathf.PerlinNoise(Time.time * 3f, 0f));
            innerGlowLight.intensity = Mathf.Lerp(0f, _initInnerIntensity, energyFactor) * pulse;
        }

        // Area Light (Dimming)
        if (areaMapLight)
        {
            areaMapLight.enabled = true;
            // Linear scaling: 50% energy = 50% intensity
            areaMapLight.intensity = Mathf.Lerp(0f, _initAreaIntensity, energyFactor);
            areaMapLight.range = Mathf.Lerp(5f, _initAreaRange, energyFactor);
        }
    }

    // --- 2. INTERACTION & COLLECTION ---
    void UpdateInteractions()
    {
        float detectionRange = areaMapLight != null ? areaMapLight.range : 5f;
        
        HashSet<ILitObject> visibleObjects = new HashSet<ILitObject>();
        HashSet<TombstoneController> visibleTombstones = new HashSet<TombstoneController>();

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, detectionLayer);

        foreach (var col in hits)
        {
            if (col == null) continue;

            Vector3 targetCenter = col.bounds.center;
            Vector3 dir = targetCenter - transform.position;
            if (!Physics.Raycast(transform.position, dir, dir.magnitude, obstructionLayer))
            {
                ILitObject litObj = col.GetComponent<ILitObject>();
                if (litObj != null)
                {
                    visibleObjects.Add(litObj);
                    if (litObj is TombstoneController tomb && tomb.CurrentEnergy > 0)
                    {
                        visibleTombstones.Add(tomb);
                    }
                }
            }
        }

        TombstoneController bestTombstone = null;
        if (energyFactor < energyThreshold)
        {
            float minDst = float.MaxValue;
            foreach (var t in visibleTombstones)
            {
                float d = Vector3.Distance(transform.position, t.transform.position);
                if (d < minDst)
                {
                    minDst = d;
                    bestTombstone = t;
                }
            }
        }

        foreach (var obj in visibleObjects)
        {
            if (obj is TombstoneController tomb)
            {
                if (tomb == bestTombstone)
                {
                    if (!_currentlyLitObjects.Contains(tomb)) tomb.OnLit(LightSourceType.Wisp);
                }
                else
                {
                    if (_currentlyLitObjects.Contains(tomb)) tomb.OnUnlit(LightSourceType.Wisp);
                }
            }
            else
            {
                if (!_currentlyLitObjects.Contains(obj)) obj.OnLit(LightSourceType.Wisp);
            }
        }

        foreach (var oldObj in _currentlyLitObjects)
        {
            if (!visibleObjects.Contains(oldObj))
            {
                oldObj.OnUnlit(LightSourceType.Wisp);
            }
        }

        _currentlyLitObjects.Clear();
        foreach (var obj in visibleObjects)
        {
            if (obj is TombstoneController tomb)
            {
                if (tomb == bestTombstone) _currentlyLitObjects.Add(obj);
            }
            else
            {
                _currentlyLitObjects.Add(obj);
            }
        }
        
        _currentTargetTombstone = bestTombstone;
    }

    void HandleDeath()
    {
        if (spriteRenderer) spriteRenderer.enabled = false;
        if (innerGlowLight) innerGlowLight.enabled = false;
        if (areaMapLight) areaMapLight.enabled = false;

        foreach (var obj in _currentlyLitObjects)
        {
            obj.OnUnlit(LightSourceType.Wisp);
        }
        _currentlyLitObjects.Clear();
        _currentTargetTombstone = null;
    }
}