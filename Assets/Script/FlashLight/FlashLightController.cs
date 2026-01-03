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

    void Start()
    {
        if (Camera.main != null) mainCam = Camera.main.transform;
        if (spotLight != null) spotLight.enabled = false;
        
        if (playerTransform != null && mainCam != null) UpdateTargetTransform(1000f);
    }

    void LateUpdate()
    {
        if (playerTransform == null || mainCam == null) return;
        UpdateTargetTransform(Time.deltaTime);
    }

    void Update()
    {
        // 1. Get Desired State directly from Input Manager
        bool isInputHeld = InputManager.Instance.IsFlashlightHeld;
        
        // 2. Wisp Power Check (Fail-safe)
        bool canBeOn = true;
        if (WispController.Instance != null && !WispController.Instance.IsWispAlive)
        {
            canBeOn = false;
        }

        // 3. Final Should-Be-On State
        bool shouldBeOn = isInputHeld && canBeOn;

        // 4. State Sync (Only run logic if state CHANGES)
        if (shouldBeOn != IsActive)
        {
            SetFlashlightState(shouldBeOn);
        }

        // 5. Logic Raycast
        if (IsActive) CheckLightInteraction();
    }

    void SetFlashlightState(bool on)
    {
        IsActive = on;
        if (spotLight != null) spotLight.enabled = on;

        // --- NEW: Notify Energy Manager ---
        if (LightEnergyManager.Instance != null)
        {
            LightEnergyManager.Instance.SetFlashlightState(on);
        }
        // ----------------------------------

        if (on)
        {
            TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseWeak);
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