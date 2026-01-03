using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light spotLight;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController; 

    [Header("Positioning")]
    [SerializeField] private Vector3 offset = new Vector3(0.5f, 1.5f, 0f);

    [Header("Motion & Delay")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private float bobFrequency = 5f;
    [SerializeField] private float bobAmplitude = 0.05f;

    [Header("Logic")]
    [SerializeField] private LayerMask interactLayer;

    // State
    public bool IsActive { get; private set; }
    private ILitObject currentLitObj;
    private Transform mainCam;
    
    // Dimming Variables
    private float _initIntensity;
    private float _initRange;
    private float _minRange = 5.0f; // Minimum range when energy is near 0

    void Start()
    {
        if (Camera.main != null) mainCam = Camera.main.transform;
        
        if (spotLight != null) 
        {
            // 1. Cache the starting values set in the Inspector
            _initIntensity = spotLight.intensity;
            _initRange = spotLight.range;
            
            spotLight.enabled = false;
        }
        
        if (playerTransform != null && mainCam != null) UpdateTargetTransform(1000f);
    }

    void LateUpdate()
    {
        if (playerTransform == null || mainCam == null) return;
        
        UpdateTargetTransform(Time.deltaTime);

        if (IsActive)
        {
            UpdateBrightness();
        }
    }

    void Update()
    {
        bool shouldBeOn = InputManager.Instance.IsFlashlightHeld;

        if (WispController.Instance != null && !WispController.Instance.IsWispAlive) 
        {
            shouldBeOn = false;
        }

        if (shouldBeOn != IsActive)
        {
            SetFlashlightState(shouldBeOn);
        }

        if (IsActive) CheckLightInteraction();
    }

    void UpdateBrightness()
    {
        if (spotLight == null || LightEnergyManager.Instance == null) return;
        float energyFactor = LightEnergyManager.Instance.EnergyFraction;

        spotLight.intensity = Mathf.Lerp(0f, _initIntensity, energyFactor);
        spotLight.range = Mathf.Lerp(_minRange, _initRange, energyFactor);
    }

    void SetFlashlightState(bool on)
    {
        IsActive = on;
        if (spotLight != null) spotLight.enabled = on;

        if (LightEnergyManager.Instance != null)
        {
            LightEnergyManager.Instance.SetFlashlightState(on);
        }

        if (on)
        {
            TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseWeak);
            UpdateBrightness(); // Update immediately on turn on so it doesn't flash wrong
        }
        else
        {
            if (currentLitObj != null)
            {
                currentLitObj.OnUnlit(LightSourceType.Flashlight);
                currentLitObj = null;
            }
        }
    }

    void UpdateTargetTransform(float dt)
    {
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

    void CheckLightInteraction()
    {
        if (spotLight == null) return;
        
        // Raycast uses current dynamic range
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, spotLight.range, interactLayer))
        {
            ILitObject obj = hit.collider.GetComponent<ILitObject>();
            if (obj != currentLitObj)
            {
                if (currentLitObj != null) currentLitObj.OnUnlit(LightSourceType.Flashlight);
                currentLitObj = obj;
                if (currentLitObj != null) currentLitObj.OnLit(LightSourceType.Flashlight);
            }
        }
        else
        {
            if (currentLitObj != null)
            {
                currentLitObj.OnUnlit(LightSourceType.Flashlight);
                currentLitObj = null;
            }
        }
    }
}