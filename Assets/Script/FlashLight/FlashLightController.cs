using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light spotLight;
    [SerializeField] private Transform playerTransform;
    [Tooltip("Optional: Drag PlayerController here to sync bobbing with movement speed.")]
    [SerializeField] private PlayerController playerController; 

    [Header("Positioning")]
    [SerializeField] private Vector3 offset = new Vector3(0.5f, 1.5f, 0f);

    [Header("Motion & Delay")]
    [Tooltip("Higher = Tighter, Lower = More Lag/Sway")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [Tooltip("Higher = Tighter, Lower = More Lag/Sway")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    [Header("Bobbing (The 'Bound')")]
    [SerializeField] private float bobFrequency = 5f;
    [SerializeField] private float bobAmplitude = 0.05f;

    [Header("Logic")]
    [SerializeField] private float duration = 10.0f;
    [SerializeField] private float cooldown = 3.0f;
    [SerializeField] private LayerMask interactLayer;

    // State
    public bool IsActive { get; private set; }
    private float turnOffTime;
    private float nextAvailableTime;
    private ILitObject currentLitObj;
    private Transform mainCam;

    void Start()
    {
        if (Camera.main != null) mainCam = Camera.main.transform;
        if (spotLight != null) spotLight.enabled = false;
        
        // Snap immediately on start to prevent weird flying in from (0,0,0)
        if (playerTransform != null && mainCam != null)
        {
            UpdateTargetTransform(1000f); // Instant snap
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnWispCycleTriggered += TryToggle;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnWispCycleTriggered -= TryToggle;
    }

    // --- MOVEMENT LOGIC ---
    void LateUpdate()
    {
        if (playerTransform == null || mainCam == null) return;

        UpdateTargetTransform(Time.deltaTime);

        if (IsActive) CheckLightInteraction();
    }

    void UpdateTargetTransform(float dt)
    {
        // 1. ROTATION LAG (Slerp)
        // We smooth towards the camera's rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, mainCam.rotation, dt * rotationSmoothSpeed);

        // 2. CALCULATE TARGET POSITION
        Vector3 targetPos = playerTransform.position;
        targetPos += mainCam.right * offset.x; 
        targetPos += Vector3.up * offset.y;     
        targetPos += mainCam.forward * offset.z; 

        // 3. APPLY BOBBING (The "Bound")
        float currentBobFreq = bobFrequency;
        float currentBobAmp = bobAmplitude;

        // If we have player controller, boost bobbing when moving
        if (playerController != null)
        {
            if (playerController.CurrentHorizontalSpeed > 0.1f)
            {
                currentBobFreq *= 2f; // Bob faster when moving
                currentBobAmp *= 1.5f; // Bob wider when moving
            }
        }

        // Calculate sine wave offset
        float bobY = Mathf.Sin(Time.time * currentBobFreq) * currentBobAmp;
        targetPos.y += bobY;

        // 4. POSITION LAG (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPos, dt * positionSmoothSpeed);
    }

    // --- TOGGLE LOGIC ---
    void TryToggle()
    {
        if (IsActive) TurnOff();
        else TurnOn();
    }

    void TurnOn()
    {
        if (WispController.Instance != null && !WispController.Instance.IsWispAlive) return;
        if (Time.time < nextAvailableTime) return;

        IsActive = true;
        turnOffTime = Time.time + duration;
        
        if (spotLight != null) spotLight.enabled = true;
        TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseWeak);
    }

    void TurnOff()
    {
        if (!IsActive) return;
        IsActive = false;
        nextAvailableTime = Time.time + cooldown;
        
        if (spotLight != null) spotLight.enabled = false;

        if (currentLitObj != null)
        {
            currentLitObj.OnUnlit(LightSourceType.Flashlight);
            currentLitObj = null;
        }
    }

    void Update()
    {
        if (IsActive && Time.time >= turnOffTime) TurnOff();
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