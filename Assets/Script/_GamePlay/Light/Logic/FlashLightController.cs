using UnityEngine;
using System.Collections.Generic;

public class FlashlightController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BoolVariableSO isFlashlightOn;
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [SerializeField] private TransformAnchorSO playerAnchor;
    [Header("References")]
    [SerializeField] private Light spotLight;
    [SerializeField] private PlayerController playerController; 

    [Header("Positioning")]
    [SerializeField] private Vector3 offset = new Vector3(0.5f, 1.5f, 0f);

    [Header("Motion & Delay")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private float bobFrequency = 5f;
    [SerializeField] private float bobAmplitude = 0.05f;

    [Header("Logic")]
    [SerializeField] private LayerMask interactLayer;   // Monsters, Eyes, Interactables
    [SerializeField] private LayerMask obstructionLayer; // Walls, Ground (Blocks light)

    // State
    public bool IsActive { get; private set; }
    private Transform mainCam;
    
    // Changed from single object to a List to handle multiple things in the cone
    private HashSet<ILitObject> currentlyLitObjects = new HashSet<ILitObject>();

    // Dimming Variables
    private float _initIntensity;
    private float _initRange;
    private float _minRange = 5.0f;

    void Start()
    {
        if (Camera.main != null) mainCam = Camera.main.transform;
        
        if (spotLight != null) 
        {
            _initIntensity = spotLight.intensity;
            _initRange = spotLight.range;
            spotLight.enabled = false;
        }
        
        UpdateTargetTransform(1000f);
    }

    void LateUpdate()
    {
        if (playerAnchor == null || playerAnchor.Value == null || mainCam == null) return;
        UpdateTargetTransform(Time.deltaTime);

        if (IsActive)
        {
            UpdateBrightness();
        }
    }

    void Update()
    {
        bool shouldBeOn = InputManager.Instance.IsFlashlightHeld;

        if (currentEnergy.Value<=0) 
        {
            shouldBeOn = false;
        }

        if (shouldBeOn != IsActive)
        {
            SetFlashlightState(shouldBeOn);
        }

        if (IsActive) CheckConeInteraction();
    }

    void UpdateBrightness()
    {
        float energyFactor = currentEnergy.Value/maxEnergy.Value;

        spotLight.intensity = Mathf.Lerp(0f, _initIntensity, energyFactor);
        spotLight.range = Mathf.Lerp(_minRange, _initRange, energyFactor);
    }

    void SetFlashlightState(bool on)
    {
        IsActive = on;
        if (spotLight != null) spotLight.enabled = on;

        if (isFlashlightOn != null)
                    isFlashlightOn.Value = on;

        if (on)
        {
            UpdateBrightness();
        }
        else
        {
            // Clear all lit objects when turning off
            foreach (var obj in currentlyLitObjects)
            {
                obj.OnUnlit(LightSourceType.Flashlight);
            }
            currentlyLitObjects.Clear();
        }
    }

    void UpdateTargetTransform(float dt)
    {
        Transform playerTransform = playerAnchor.Value;
        transform.rotation = Quaternion.Slerp(transform.rotation, mainCam.rotation, dt * rotationSmoothSpeed);
        Vector3 targetPos = playerTransform.position;
        targetPos += mainCam.right * offset.x; 
        targetPos += Vector3.up * offset.y;     
        targetPos += mainCam.forward * offset.z; 

        float currentBobFreq = bobFrequency;
        float currentBobAmp = bobAmplitude;

        if (playerController != null && playerController.CurrentHorizontalSpeed > 0.1f)
        {
            currentBobFreq *= 2f; 
            currentBobAmp *= 1.5f; 
        }

        float bobY = Mathf.Sin(Time.time * currentBobFreq) * currentBobAmp;
        targetPos.y += bobY;
        transform.position = Vector3.Lerp(transform.position, targetPos, dt * positionSmoothSpeed);
    }

    // --- NEW: CONE LOGIC ---
    void CheckConeInteraction()
    {
        if (spotLight == null) return;

        float range = spotLight.range;
        float halfAngle = spotLight.spotAngle * 0.5f;

        // 1. Find everything in Range
        Collider[] hits = Physics.OverlapSphere(transform.position, range, interactLayer);
        HashSet<ILitObject> visibleThisFrame = new HashSet<ILitObject>();

        foreach (var hit in hits)
        {
            // 2. Direction Check
            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            
            // 3. Angle Check (Is it inside the cone?)
            if (Vector3.Angle(transform.forward, dirToTarget) < halfAngle)
            {
                // 4. Line of Sight Check (Is it behind a wall?)
                float dst = Vector3.Distance(transform.position, hit.transform.position);
                if (!Physics.Raycast(transform.position, dirToTarget, dst, obstructionLayer))
                {
                    ILitObject litObj = hit.GetComponent<ILitObject>();
                    if (litObj != null)
                    {
                        visibleThisFrame.Add(litObj);
                    }
                }
            }
        }

        // 5. Apply States
        
        // A. Handle New/Persisting Objects
        foreach (var obj in visibleThisFrame)
        {
            if (!currentlyLitObjects.Contains(obj))
            {
                obj.OnLit(LightSourceType.Flashlight);
            }
        }

        // B. Handle Objects that left the cone (or got blocked)
        foreach (var oldObj in currentlyLitObjects)
        {
            if (!visibleThisFrame.Contains(oldObj))
            {
                oldObj.OnUnlit(LightSourceType.Flashlight);
            }
        } 

        // C. Update Cache
        currentlyLitObjects = visibleThisFrame;
    }
}